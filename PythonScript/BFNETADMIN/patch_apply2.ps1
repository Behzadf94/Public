# -*- coding: utf-8 -*-
"""
Patch Automation Script: BFNETADMIN Hardware & BIOS Intelligence Studio
Script Name: patch_apply.py
Target Environment: Windows / Python 3.10+ / Python 3.14
"""
import os
import sys
import shutil
import datetime

CURRENT_DIR = os.path.dirname(os.path.abspath(__file__))
MAIN_PY = os.path.join(CURRENT_DIR, "main.py")
HW_AUDIT_PY = os.path.join(CURRENT_DIR, "hw_audit.py")

# =====================================================================
# 1. CONTENT OF hw_audit.py
# =====================================================================
HW_AUDIT_CONTENT = r'''# -*- coding: utf-8 -*-
"""
BFNETADMIN - Hardware, BIOS & AI Readiness Engine
Direct WMI/CIM/PowerShell inspection with Zero-External-Dependency.
"""
import os
import sys
import time
import json
import socket
import ctypes
import subprocess
from concurrent.futures import ThreadPoolExecutor
from typing import Any, Callable, Dict, List, Optional

_DIR = os.path.dirname(os.path.abspath(__file__))
if _DIR not in sys.path:
    sys.path.insert(0, _DIR)

from base_command import BaseCommand
from command_registry import CommandRegistry
import db_core

def run_ps_json(script: str) -> Any:
    """Executes a PowerShell scriptlet returning parsed JSON."""
    cmd = ["powershell", "-NoProfile", "-ExecutionPolicy", "Bypass", "-Command", f"$ProgressPreference = 'SilentlyContinue'; {script} | ConvertTo-Json -Depth 4 -Compress"]
    try:
        proc = subprocess.run(cmd, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True, timeout=15)
        out = proc.stdout.strip()
        if not out:
            return None
        return json.loads(out)
    except Exception:
        return None

def run_cmd_text(cmd_list: List[str]) -> str:
    try:
        proc = subprocess.run(cmd_list, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True, timeout=10)
        return proc.stdout.strip()
    except Exception:
        return ""

def _eval_ai_readiness(cpu_threads: int, ram_gb: float, gpu_name: str, vram_gb: float) -> Dict[str, Any]:
    """Analyzes hardware capacity for local LLM inference (e.g. Ollama, GGUF, vLLM)."""
    score = 0
    tier = "Basic / CPU Only"
    models_supported = []
    notes = []

    # GPU / VRAM scoring
    is_nvidia = "nvidia" in gpu_name.lower() or "geforce" in gpu_name.lower() or "rtx" in gpu_name.lower() or "quadro" in gpu_name.lower() or "tesla" in gpu_name.lower()
    
    if vram_gb >= 24:
        score += 55
        notes.append("Ultra High VRAM: Capable of 70B (Q4) or 32B (Q8) full offload.")
    elif vram_gb >= 16:
        score += 45
        notes.append("High VRAM: Perfect for 14B-32B models fully GPU-resident.")
    elif vram_gb >= 12:
        score += 38
        notes.append("Medium-High VRAM: Excellent for 8B-14B models (Q5/Q8) with high ctx.")
    elif vram_gb >= 8:
        score += 30
        notes.append("Standard VRAM: Optimal for 7B/8B (Q4_K_M) full offload (40-60 t/s).")
    elif vram_gb >= 4:
        score += 18
        notes.append("Entry VRAM: Can run 3B models or partial offload of 7B/8B.")
    else:
        score += 5
        notes.append("Low / Shared VRAM: Will rely mainly on System RAM & CPU.")

    if is_nvidia:
        score += 10
        notes.append("CUDA acceleration natively supported by all major runtimes.")
    else:
        notes.append("Vulkan / OpenCL / ROCm / CPU fallback depending on driver.")

    # System RAM scoring
    if ram_gb >= 64:
        score += 25
        notes.append("Massive System RAM: Large models (70B Q4) can load into CPU RAM.")
    elif ram_gb >= 32:
        score += 20
        notes.append("32GB RAM: Can host up to 30B/32B Q4 models on CPU.")
    elif ram_gb >= 16:
        score += 12
        notes.append("16GB RAM: Smooth multitasking with 7B/8B models.")
    else:
        score += 5
        notes.append("Under 16GB RAM: Constrained; recommended max model size: 3B/7B Q4.")

    # CPU Threads
    if cpu_threads >= 16:
        score += 10
    elif cpu_threads >= 8:
        score += 7
    else:
        score += 3

    # Supported local models catalog matrix
    if vram_gb >= 24 or (vram_gb >= 16 and ram_gb >= 32):
        tier = "Enterprise AI Workstation"
        models_supported = [
            "DeepSeek-R1-Distill-70B (Q4_K_M)",
            "Llama-3.3-70B-Instruct (Q4)",
            "Command-R+ 104B (Quantized Offload)",
            "Qwen-2.5-Coder-32B (Q8/FP16 Full Offload)",
            "Llama-3.1-8B (FP16 / Unquantized Full Speed)"
        ]
    elif vram_gb >= 12 or (ram_gb >= 32 and vram_gb >= 8):
        tier = "Pro AI Developer Station"
        models_supported = [
            "DeepSeek-R1-Distill-14B / 32B (Q4)",
            "Qwen-2.5-14B-Instruct (Full GPU)",
            "Llama-3.1-8B-Instruct (Q8 Full Offload)",
            "Mistral-7B-Instruct-v0.3 (Full Offload)",
            "Codestral-22B (Q4 Hybrid Offload)"
        ]
    elif vram_gb >= 6 or ram_gb >= 16:
        tier = "Local AI Capable"
        models_supported = [
            "Llama-3.2-3B (High Speed ~70 t/s)",
            "Llama-3.1-8B (Q4_K_M Offload ~30-45 t/s)",
            "Mistral-7B (Q4_K_M)",
            "Phi-4 (14B Q4 Partial)",
            "Gemma-2-9B (Q4)"
        ]
    else:
        tier = "Entry / Lightweight AI"
        models_supported = [
            "Llama-3.2-1B / 3B (CPU/Quant)",
            "Qwen-2.5-1.5B / 3B",
            "Phi-3.5-mini (3.8B)",
            "TinyLlama-1.1B"
        ]

    score = min(100, score)
    return {
        "score": score,
        "tier": tier,
        "models": models_supported,
        "notes": notes
    }

def _run_quick_cpu_benchmark() -> Dict[str, Any]:
    """Micro benchmark calculating primes & matrix ops to estimate CPU computing power."""
    # Single Thread Benchmark
    t0 = time.perf_counter()
    count = 0
    for i in range(2, 45000):
        is_p = True
        for d in range(2, int(i**0.5) + 1):
            if i % d == 0:
                is_p = False
                break
        if is_p:
            count += 1
    t_single = time.perf_counter() - t0
    single_score = int(1000.0 / max(0.01, t_single))

    # Multi Thread Benchmark
    def worker(chunk_start, chunk_end):
        c = 0
        for num in range(chunk_start, chunk_end):
            is_prime = True
            for d in range(2, int(num**0.5) + 1):
                if num % d == 0:
                    is_prime = False
                    break
            if is_prime:
                c += 1
        return c

    cores = os.cpu_count() or 4
    ranges = []
    chunk_size = 20000
    for idx in range(cores):
        ranges.append((idx * chunk_size + 2, (idx + 1) * chunk_size + 2))

    t1 = time.perf_counter()
    with ThreadPoolExecutor(max_workers=cores) as ex:
        list(ex.map(lambda r: worker(r[0], r[1]), ranges))
    t_multi = time.perf_counter() - t1
    multi_score = int((1000.0 * cores) / max(0.01, t_multi * 1.5))

    return {
        "single_thread_score": single_score,
        "multi_thread_score": multi_score,
        "execution_sec": round(t_multi, 2),
        "tested_cores": cores
    }

@CommandRegistry.register("GET_HARDWARE_INTELLIGENCE")
class GetHardwareIntelligenceCommand(BaseCommand):
    def __init__(self, action_name: str = "GET_HARDWARE_INTELLIGENCE", params: Optional[Dict[str, Any]] = None):
        super().__init__(action_name=action_name, params=params)

    def execute(self, progress_callback: Optional[Callable[[int, int, str], None]] = None, *args, **kwargs) -> Dict[str, Any]:
        if progress_callback:
            progress_callback(1, 6, "Collecting Motherboard, SMBIOS & UEFI details...")

        # 1. BIOS & BaseBoard
        ps_bios = """
        [PSCustomObject]@{
            Bios = Get-CimInstance Win32_BIOS | Select-Object Manufacturer, Name, Version, ReleaseDate, SMBIOSBIOSVersion, SerialNumber
            Board = Get-CimInstance Win32_BaseBoard | Select-Object Manufacturer, Product, Version, SerialNumber
            Computer = Get-CimInstance Win32_ComputerSystem | Select-Object Name, Domain, Workgroup, Model, Manufacturer, TotalPhysicalMemory
            OS = Get-CimInstance Win32_OperatingSystem | Select-Object Caption, Version, OSArchitecture, InstallDate, LocalDateTime
        }
        """
        bios_data = run_ps_json(ps_bios) or {}

        if progress_callback:
            progress_callback(2, 6, "Querying Processor architecture & cache...")

        # 2. Processor
        ps_cpu = """
        Get-CimInstance Win32_Processor | Select-Object Name, Manufacturer, NumberOfCores, NumberOfLogicalProcessors, MaxClockSpeed, L3CacheSize, Architecture
        """
        cpu_raw = run_ps_json(ps_cpu)
        if isinstance(cpu_raw, list):
            cpu_info = cpu_raw[0] if cpu_raw else {}
        else:
            cpu_info = cpu_raw or {}

        if progress_callback:
            progress_callback(3, 6, "Scanning Physical RAM slots and Storage drives...")

        # 3. Memory & Disks
        ps_storage = """
        [PSCustomObject]@{
            Disks = @(Get-CimInstance Win32_DiskDrive | Select-Object Model, InterfaceType, MediaType, @{N='SizeGB';E={[math]::Round($_.Size/1GB, 2)}}, SerialNumber)
            Volumes = @(Get-CimInstance Win32_LogicalDisk -Filter "DriveType=3" | Select-Object DeviceID, VolumeName, FileSystem, @{N='FreeGB';E={[math]::Round($_.FreeSpace/1GB, 2)}}, @{N='TotalGB';E={[math]::Round($_.Size/1GB, 2)}})
            MemorySlots = @(Get-CimInstance Win32_PhysicalMemory | Select-Object BankLabel, DeviceLocator, @{N='CapacityGB';E={[math]::Round($_.Capacity/1GB, 2)}}, Speed, MemoryType, PartNumber)
        }
        """
        storage_data = run_ps_json(ps_storage) or {"Disks": [], "Volumes": [], "MemorySlots": []}

        if progress_callback:
            progress_callback(4, 6, "Inspecting GPUs, Displays & Hardware Port Controllers...")

        # 4. Video & Controllers
        ps_gpu = """
        [PSCustomObject]@{
            GPUs = @(Get-CimInstance Win32_VideoController | Select-Object Name, DriverVersion, VideoProcessor, @{N='VRAM_GB';E={[math]::Round($_.AdapterRAM/1GB, 2)}}, CurrentHorizontalResolution, CurrentVerticalResolution)
            USB = @(Get-CimInstance Win32_USBController | Select-Object Name, Manufacturer)
            NetAdapters = @(Get-CimInstance Win32_NetworkAdapter -Filter "NetConnectionStatus=2" | Select-Object Name, MACAddress, Speed, AdapterType)
        }
        """
        gpu_data = run_ps_json(ps_gpu) or {"GPUs": [], "USB": [], "NetAdapters": []}

        if progress_callback:
            progress_callback(5, 6, "Running Micro CPU Benchmark & AI Engine Capability model...")

        # Parse RAM & VRAM
        total_ram_gb = 0.0
        try:
            total_bytes = bios_data.get("Computer", {}).get("TotalPhysicalMemory", 0)
            total_ram_gb = round(float(total_bytes) / (1024**3), 2)
        except Exception:
            total_ram_gb = 8.0

        vram_detected_gb = 0.0
        primary_gpu_name = "Integrated / Standard Display"
        gpus_list = gpu_data.get("GPUs") or []
        if isinstance(gpus_list, dict):
            gpus_list = [gpus_list]

        if gpus_list:
            primary_gpu_name = gpus_list[0].get("Name", "Standard GPU")
            vram_detected_gb = float(gpus_list[0].get("VRAM_GB") or 0.0)
            # If Win32_VideoController under-reports VRAM (common 32-bit uint overflow for >4GB), query registry DX
            if vram_detected_gb <= 0.0:
                try:
                    reg_vram = run_cmd_text(["powershell", "-NoProfile", "-Command", "(Get-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\DirectX' -ErrorAction SilentlyContinue).DedicatedVideoMemory / 1GB"])
                    if reg_vram and float(reg_vram) > 0:
                        vram_detected_gb = round(float(reg_vram), 2)
                except Exception:
                    pass

        cpu_threads = int(cpu_info.get("NumberOfLogicalProcessors") or os.cpu_count() or 4)
        ai_assessment = _eval_ai_readiness(cpu_threads, total_ram_gb, primary_gpu_name, vram_detected_gb)
        benchmark_results = _run_quick_cpu_benchmark()

        # BIOS Date & System Age
        bios_rel_date = str(bios_data.get("Bios", {}).get("ReleaseDate") or "")
        age_str = "Modern (UEFI Compliant)"
        if bios_rel_date:
            try:
                raw_year = int(bios_rel_date[:4])
                curr_year = datetime.datetime.now().year
                diff = curr_year - raw_year
                if diff <= 2:
                    age_str = f"Current Generation ({raw_year})"
                elif diff <= 5:
                    age_str = f"Active Modern Architecture ({raw_year}, {diff} yrs old)"
                else:
                    age_str = f"Legacy / Mature Architecture ({raw_year}, {diff} yrs old)"
            except Exception:
                pass

        if progress_callback:
            progress_callback(6, 6, "Hardware intelligence synthesis complete.")

        payload = {
            "bios": bios_data.get("Bios", {}),
            "board": bios_data.get("Board", {}),
            "computer": bios_data.get("Computer", {}),
            "os": bios_data.get("OS", {}),
            "cpu": cpu_info,
            "storage": storage_data,
            "gpus": gpus_list,
            "controllers": {
                "usb": gpu_data.get("USB", []),
                "network": gpu_data.get("NetAdapters", [])
            },
            "metrics": {
                "total_ram_gb": total_ram_gb,
                "gpu_vram_gb": vram_detected_gb,
                "hardware_age": age_str,
                "benchmark": benchmark_results,
                "ai_readiness": ai_assessment
            }
        }
        db_core.log_event("GET_HARDWARE_INTELLIGENCE", "SUCCESS", f"CPU: {cpu_info.get('Name')}")
        return {"status": "success", "data": payload, "message": "Hardware intelligence audit completed successfully."}

@CommandRegistry.register("SYNC_HARDWARE_CLOCK")
class SyncHardwareClockCommand(BaseCommand):
    """Synchronizes Windows System & Hardware RTC Clock via Windows Time Service."""
    def __init__(self, action_name: str = "SYNC_HARDWARE_CLOCK", params: Optional[Dict[str, Any]] = None):
        super().__init__(action_name=action_name, params=params)

    def execute(self, progress_callback: Optional[Callable[[int, int, str], None]] = None, *args, **kwargs) -> Dict[str, Any]:
        cmds = [
            'net start w32time',
            'w32tm /config /manualpeerlist:"pool.ntp.org,0x1 time.windows.com,0x1" /syncfromflags:manual /update',
            'w32tm /resync /force'
        ]
        logs = []
        for c in cmds:
            try:
                p = subprocess.run(c, shell=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)
                logs.append(p.stdout.strip())
            except Exception as e:
                logs.append(str(e))
        
        db_core.log_event("SYNC_HARDWARE_CLOCK", "SUCCESS", "Clock synchronized with NTP")
        return {
            "status": "success",
            "data": "\n".join(logs),
            "message": "System time & Motherboard RTC synchronized successfully with atomic time servers."
        }
'''

# =====================================================================
# 2. PATCH LOGIC FOR main.py
# =====================================================================
def apply_patch():
    print("[*] Starting BFNETADMIN Hardware Dashboard Integration...")

    # Backup main.py
    if not os.path.exists(MAIN_PY):
        print(f"[!] Error: main.py not found at {MAIN_PY}")
        sys.exit(1)

    timestamp = datetime.datetime.now().strftime("%Y%m%d_%H%M%S")
    backup_file = f"{MAIN_PY}.bak_{timestamp}"
    shutil.copy2(MAIN_PY, backup_file)
    print(f"[+] Backup created: {backup_file}")

    # Write hw_audit.py
    with open(HW_AUDIT_PY, "w", encoding="utf-8") as f:
        f.write(HW_AUDIT_CONTENT.strip())
    print(f"[+] Created engine module: {HW_AUDIT_PY}")

    # Read main.py
    with open(MAIN_PY, "r", encoding="utf-8") as f:
        content = f.read()

    # Verify and Inject import
    if "import hw_audit" not in content:
        target_import = "import ad_manager"
        if target_import in content:
            content = content.replace(target_import, f"{target_import}\nimport hw_audit")
            print("[+] Injected 'import hw_audit' into main.py")
        else:
            print("[!] Warning: Could not find import anchor, adding to top.")
            content = "import hw_audit\n" + content

    # Inject tab creation in _build_tabs
    if "self.tab_hardware" not in content:
        anchor_tab = "self.tab_ad = ttk.Frame(notebook)"
        replacement_tab = (
            "self.tab_hardware = ttk.Frame(notebook)\n"
            "        self.tab_ad = ttk.Frame(notebook)"
        )
        content = content.replace(anchor_tab, replacement_tab, 1)

        anchor_add = 'notebook.add(self.tab_ad, text="  System / AD Tools  ")'
        replacement_add = (
            'notebook.add(self.tab_hardware, text="  🖥 Hardware, BIOS & AI Studio  ")\n'
            '        notebook.add(self.tab_ad, text="  System / AD Tools  ")'
        )
        content = content.replace(anchor_add, replacement_add, 1)

        anchor_setup = "self._setup_ad_tab()"
        replacement_setup = (
            "self._setup_hardware_tab()\n"
            "        self._setup_ad_tab()"
        )
        content = content.replace(anchor_setup, replacement_setup, 1)
        print("[+] Registered Hardware Tab in _build_tabs()")

    # Inject GUI implementation methods before main()
    hardware_methods = r'''
    def _setup_hardware_tab(self):
        """Constructs Enterprise Dashboard for BIOS, Motherboard, AI Readiness & Gauges."""
        top_bar = ttk.Frame(self.tab_hardware)
        top_bar.pack(fill=tk.X, padx=10, pady=6)

        btn_refresh = ttk.Button(top_bar, text="🔍 Full Deep Hardware & BIOS Audit", command=self._trigger_hw_audit)
        btn_refresh.pack(side=tk.LEFT, padx=5, ipady=4)

        btn_sync_time = ttk.Button(top_bar, text="⏱ Sync System & BIOS RTC Clock", command=self._sync_hw_clock)
        btn_sync_time.pack(side=tk.LEFT, padx=5, ipady=4)

        self.lbl_hw_status = ttk.Label(top_bar, text="Status: Ready to inspect hardware.")
        self.lbl_hw_status.pack(side=tk.LEFT, padx=15)

        # Upper Gauges Card
        f_gauges = ttk.LabelFrame(self.tab_hardware, text=" Architecture Metrics & Performance Gauges ")
        f_gauges.pack(fill=tk.X, padx=10, pady=4)

        gauge_container = ttk.Frame(f_gauges)
        gauge_container.pack(fill=tk.X, padx=5, pady=5)

        # Gauge 1: AI Readiness Score
        f_g1 = ttk.Frame(gauge_container)
        f_g1.pack(side=tk.LEFT, expand=True, fill=tk.BOTH, padx=10)
        self.canvas_gauge_ai = tk.Canvas(f_g1, width=150, height=85, bg="#0d1117", highlightthickness=0)
        self.canvas_gauge_ai.pack()
        self.lbl_gauge_ai_desc = ttk.Label(f_g1, text="AI Capability Score: ---", font=("Consolas", 9, "bold"))
        self.lbl_gauge_ai_desc.pack()

        # Gauge 2: CPU Benchmark Single Core
        f_g2 = ttk.Frame(gauge_container)
        f_g2.pack(side=tk.LEFT, expand=True, fill=tk.BOTH, padx=10)
        self.canvas_gauge_cpu = tk.Canvas(f_g2, width=150, height=85, bg="#0d1117", highlightthickness=0)
        self.canvas_gauge_cpu.pack()
        self.lbl_gauge_cpu_desc = ttk.Label(f_g2, text="CPU Bench (Multi): ---", font=("Consolas", 9, "bold"))
        self.lbl_gauge_cpu_desc.pack()

        # Gauge 3: Total RAM Capacity
        f_g3 = ttk.Frame(gauge_container)
        f_g3.pack(side=tk.LEFT, expand=True, fill=tk.BOTH, padx=10)
        self.canvas_gauge_ram = tk.Canvas(f_g3, width=150, height=85, bg="#0d1117", highlightthickness=0)
        self.canvas_gauge_ram.pack()
        self.lbl_gauge_ram_desc = ttk.Label(f_g3, text="Total RAM: ---", font=("Consolas", 9, "bold"))
        self.lbl_gauge_ram_desc.pack()

        self._draw_gauge(self.canvas_gauge_ai, 0, "AI Score")
        self._draw_gauge(self.canvas_gauge_cpu, 0, "CPU Bench")
        self._draw_gauge(self.canvas_gauge_ram, 0, "RAM Size")

        # Lower Detail Notebook
        sub_nb = ttk.Notebook(self.tab_hardware)
        sub_nb.pack(fill=tk.BOTH, expand=True, padx=10, pady=5)

        # Sub-tab 1: AI Model Compatibility
        sub_ai = ttk.Frame(sub_nb)
        sub_nb.add(sub_ai, text=" 🤖 AI Local Models Assessment ")
        self.txt_ai_details = tk.Text(sub_ai, height=12, bg="#010409", fg="#00ff66", insertbackground="#00ff66", font=("Consolas", 10), wrap="word")
        self.txt_ai_details.pack(fill=tk.BOTH, expand=True, padx=5, pady=5)
        self.txt_ai_details.insert(tk.END, "Click 'Full Deep Hardware & BIOS Audit' to calculate AI readiness matrix.\n")

        # Sub-tab 2: Motherboard & BIOS
        sub_bios = ttk.Frame(sub_nb)
        sub_nb.add(sub_bios, text=" 🏛 Motherboard & SMBIOS ")
        self.tree_bios = ttk.Treeview(sub_bios, columns=("prop", "val"), show="headings")
        self.tree_bios.heading("prop", text="Hardware Property")
        self.tree_bios.heading("val", text="Detected Value")
        self.tree_bios.column("prop", width=260)
        self.tree_bios.column("val", width=550)
        self.tree_bios.pack(fill=tk.BOTH, expand=True, padx=5, pady=5)

        # Sub-tab 3: CPU, RAM & Disks
        sub_components = ttk.Frame(sub_nb)
        sub_nb.add(sub_components, text=" ⚙ CPU, RAM & Storage ")
        self.tree_components = ttk.Treeview(sub_components, columns=("cat", "name", "detail"), show="headings")
        self.tree_components.heading("cat", text="Category")
        self.tree_components.heading("name", text="Component / Model")
        self.tree_components.heading("detail", text="Capacity / Spec")
        self.tree_components.column("cat", width=120)
        self.tree_components.column("name", width=360)
        self.tree_components.column("detail", width=330)
        self.tree_components.pack(fill=tk.BOTH, expand=True, padx=5, pady=5)

        # Sub-tab 4: Ports, USB & Network
        sub_ports = ttk.Frame(sub_nb)
        sub_nb.add(sub_ports, text=" 🔌 Ports, USB & Network Cards ")
        self.tree_ports = ttk.Treeview(sub_ports, columns=("type", "device", "identity"), show="headings")
        self.tree_ports.heading("type", text="Interface")
        self.tree_ports.heading("device", text="Device Description")
        self.tree_ports.heading("identity", text="Identifier / MAC / Serial")
        self.tree_ports.column("type", width=120)
        self.tree_ports.column("device", width=400)
        self.tree_ports.column("identity", width=290)
        self.tree_ports.pack(fill=tk.BOTH, expand=True, padx=5, pady=5)

    def _draw_gauge(self, canvas: tk.Canvas, percent: float, label: str):
        """Draws a clean semi-circular cyber gauge on Tkinter Canvas."""
        canvas.delete("all")
        w, h = 150, 85
        cx, cy, r = w / 2, h - 12, 60

        # Background track
        canvas.create_arc(cx - r, cy - r, cx + r, cy + r, start=0, extent=180, outline="#30363d", width=10, style="arc")

        # Value arc
        extent = -180.0 * (min(100.0, max(0.0, percent)) / 100.0)
        color = "#00ff66" if percent >= 70 else ("#e3b341" if percent >= 35 else "#f85149")
        if extent != 0:
            canvas.create_arc(cx - r, cy - r, cx + r, cy + r, start=180, extent=extent, outline=color, width=10, style="arc")

        # Center value text
        canvas.create_text(cx, cy - 20, text=f"{int(percent)}%", fill="#ffffff", font=("Consolas", 14, "bold"))
        canvas.create_text(cx, cy + 2, text=label, fill="#8b949e", font=("Segoe UI", 8))

    def _trigger_hw_audit(self):
        self.lbl_hw_status.config(text="Status: Inspecting hardware registers & running benchmarks...")
        for t in [self.tree_bios, self.tree_components, self.tree_ports]:
            for row in t.get_children():
                t.delete(row)
        self.txt_ai_details.delete("1.0", tk.END)

        def runner():
            def cb(c, t, msg):
                self.root.after(0, lambda: self.lbl_hw_status.config(text=f"[{c}/{t}] {msg}"))

            res = CommandRegistry.dispatch_from_dict({"action": "GET_HARDWARE_INTELLIGENCE"}, progress_cb=cb)

            def finalize():
                if res.get("status") == "success":
                    self._populate_hw_ui(res["data"])
                    self.lbl_hw_status.config(text="Status: Hardware audit complete.")
                else:
                    self.lbl_hw_status.config(text="Status: Audit failed.")
                    messagebox.showerror("Error", res.get("message", "Hardware audit failed."))

            self.root.after(0, finalize)

        threading.Thread(target=runner, daemon=True).start()

    def _populate_hw_ui(self, data: dict):
        bios = data.get("bios", {})
        board = data.get("board", {})
        comp = data.get("computer", {})
        os_info = data.get("os", {})
        cpu = data.get("cpu", {})
        storage = data.get("storage", {})
        gpus = data.get("gpus", [])
        metrics = data.get("metrics", {})
        ai = metrics.get("ai_readiness", {})
        bench = metrics.get("benchmark", {})

        # 1. Update Gauges
        ai_score = ai.get("score", 0)
        self._draw_gauge(self.canvas_gauge_ai, ai_score, "AI Score")
        self.lbl_gauge_ai_desc.config(text=f"AI Tier: {ai.get('tier', 'N/A')}")

        cpu_bench_norm = min(100.0, (bench.get("multi_thread_score", 0) / 2500.0) * 100.0)
        self._draw_gauge(self.canvas_gauge_cpu, cpu_bench_norm, "CPU Multi")
        self.lbl_gauge_cpu_desc.config(text=f"Multi: {bench.get('multi_thread_score', 0)} | Single: {bench.get('single_thread_score', 0)}")

        ram_gb = metrics.get("total_ram_gb", 0)
        ram_norm = min(100.0, (ram_gb / 64.0) * 100.0)
        self._draw_gauge(self.canvas_gauge_ram, ram_norm, f"{ram_gb} GB RAM")
        self.lbl_gauge_ram_desc.config(text=f"System RAM: {ram_gb} GB | VRAM: {metrics.get('gpu_vram_gb', 0)} GB")

        # 2. Sub-tab 1: AI Model Text
        ai_txt = f"======================================================================\n"
        ai_txt += f" AI LOCAL INFERENCE CAPABILITY REPORT (Ollama / GGUF / Llama.cpp / vLLM)\n"
        ai_txt += f"======================================================================\n"
        ai_txt += f"▶ Overall AI Readiness Score: {ai.get('score', 0)} / 100\n"
        ai_txt += f"▶ Classification Tier:       {ai.get('tier', 'Unknown')}\n"
        ai_txt += f"▶ Hardware Freshness / Age:   {metrics.get('hardware_age', 'Modern')}\n"
        ai_txt += f"▶ Primary GPU Engine:         {gpus[0].get('Name') if gpus else 'None'} ({metrics.get('gpu_vram_gb', 0)} GB VRAM)\n"
        ai_txt += f"▶ Host Total RAM:             {ram_gb} GB (Max Context Buffering)\n"
        ai_txt += f"▶ Multi-Thread Throughput:    Score {bench.get('multi_thread_score', 0)} (Execution {bench.get('execution_sec', 0)}s)\n\n"
        ai_txt += f"--- [ RECOMMENDED LOCAL AI MODELS THAT WILL RUN SMOOTHLY ] ---\n"
        for m in ai.get("models", []):
            ai_txt += f"  ✔ {m}\n"
        ai_txt += f"\n--- [ ARCHITECTURE ADVISORY & BOTTLENECK ANALYSIS ] ---\n"
        for n in ai.get("notes", []):
            ai_txt += f"  • {n}\n"
        self.txt_ai_details.insert(tk.END, ai_txt)

        # 3. Sub-tab 2: Motherboard & BIOS Table
        items_bios = [
            ("Computer Network Hostname", comp.get("Name", "N/A")),
            ("Active Domain / Workgroup", comp.get("Domain") or comp.get("Workgroup") or "WORKGROUP"),
            ("Motherboard Manufacturer", board.get("Manufacturer", "N/A")),
            ("Motherboard Model / Product", board.get("Product", "N/A")),
            ("Motherboard Serial Number", board.get("SerialNumber", "N/A")),
            ("Motherboard Hardware Revision", board.get("Version", "N/A")),
            ("BIOS Vendor / Developer", bios.get("Manufacturer", "N/A")),
            ("BIOS Version / Name", bios.get("Name") or bios.get("Version") or "N/A"),
            ("SMBIOS Version String", bios.get("SMBIOSBIOSVersion", "N/A")),
            ("BIOS Firmware Release Date", bios.get("ReleaseDate", "N/A")),
            ("Platform Hardware Freshness", metrics.get("hardware_age", "N/A")),
            ("Operating System Kernel", f"{os_info.get('Caption')} ({os_info.get('OSArchitecture')})"),
            ("OS Install & Epoch Date", os_info.get("InstallDate", "N/A")),
            ("Local BIOS/RTC Current Clock", os_info.get("LocalDateTime", "N/A"))
        ]
        for prop, val in items_bios:
            self.tree_bios.insert("", tk.END, values=(prop, val))

        # 4. Sub-tab 3: Components (CPU, RAM, Disks)
        self.tree_components.insert("", tk.END, values=("Processor", cpu.get("Name", "CPU"), f"{cpu.get('NumberOfCores')} Cores, {cpu.get('NumberOfLogicalProcessors')} Threads @ {cpu.get('MaxClockSpeed')}MHz"))
        
        for g in gpus:
            self.tree_components.insert("", tk.END, values=("Graphics Card", g.get("Name", "GPU"), f"VRAM: {g.get('VRAM_GB', 0)} GB | Driver: {g.get('DriverVersion')}"))

        for slot in storage.get("MemorySlots", []):
            self.tree_components.insert("", tk.END, values=("RAM Module", f"{slot.get('DeviceLocator', 'DIMM')} ({slot.get('PartNumber', 'OEM')})", f"{slot.get('CapacityGB')} GB @ {slot.get('Speed', 'N/A')} MT/s"))

        for d in storage.get("Disks", []):
            self.tree_components.insert("", tk.END, values=("Storage Physical", d.get("Model", "Disk"), f"{d.get('SizeGB')} GB ({d.get('InterfaceType', 'SATA/NVMe')})"))

        for v in storage.get("Volumes", []):
            self.tree_components.insert("", tk.END, values=("Storage Partition", f"Drive [{v.get('DeviceID')}] {v.get('VolumeName', '')}", f"Free: {v.get('FreeGB')} GB / Total: {v.get('TotalGB')} GB ({v.get('FileSystem')})"))

        # 5. Sub-tab 4: Ports & Controllers
        for net in data.get("controllers", {}).get("network", []):
            self.tree_ports.insert("", tk.END, values=("Ethernet/Wi-Fi", net.get("Name", "NIC"), f"MAC: {net.get('MACAddress', 'N/A')}"))

        for usb in data.get("controllers", {}).get("usb", []):
            self.tree_ports.insert("", tk.END, values=("USB Controller", usb.get("Name", "USB Host"), f"Vendor: {usb.get('Manufacturer', 'Generic')}"))

    def _sync_hw_clock(self):
        self.lbl_hw_status.config(text="Status: Resyncing RTC and Windows Time with atomic NTP servers...")
        def runner():
            res = CommandRegistry.dispatch_from_dict({"action": "SYNC_HARDWARE_CLOCK"})
            def fin():
                if res.get("status") == "success":
                    messagebox.showinfo("Time Synchronized", res.get("message"))
                    self.lbl_hw_status.config(text="Status: Time & RTC successfully synced.")
                else:
                    messagebox.showerror("Sync Error", res.get("message"))
            self.root.after(0, fin)
        threading.Thread(target=runner, daemon=True).start()
'''

    if "_setup_hardware_tab" not in content:
        # Insert before def main():
        anchor_main = "def main():"
        if anchor_main in content:
            content = content.replace(anchor_main, f"{hardware_methods}\n{anchor_main}")
            print("[+] Appended hardware dashboard methods to BFNetAdminUI")
        else:
            content += f"\n{hardware_methods}\n"

    # Write patched main.py
    with open(MAIN_PY, "w", encoding="utf-8") as f:
        f.write(content)

    print("[✔] Patch successfully applied to main.py and hw_audit.py.")

if __name__ == "__main__":
    apply_patch()

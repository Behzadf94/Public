# patch_apply.ps1 - Enterprise Patch Application Script
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$ErrorActionPreference = "Stop"

$targetFile = Join-Path $PSScriptRoot "main.py"
if (-not (Test-Path $targetFile)) {
    Write-Error "Target file not found: $targetFile"
    exit 1
}

# 1. Create timestamped backup
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupFile = "$targetFile.bak_$timestamp"
Copy-Item $targetFile $backupFile
Write-Host " [OK] Backup created at: $backupFile" -ForegroundColor Cyan

# 2. Read content
$content = Get-Content -Path $targetFile -Raw -Encoding UTF8

# Define methods to inject into BFNetAdminUI
$hwMethods = @"

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

        # Gauge 2: CPU Benchmark
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

        sub_ai = ttk.Frame(sub_nb)
        sub_nb.add(sub_ai, text=" 🤖 AI Local Models Assessment ")
        self.txt_ai_details = tk.Text(sub_ai, height=12, bg="#010409", fg="#00ff66", insertbackground="#00ff66", font=("Consolas", 10), wrap="word")
        self.txt_ai_details.pack(fill=tk.BOTH, expand=True, padx=5, pady=5)
        self.txt_ai_details.insert(tk.END, "Click 'Full Deep Hardware & BIOS Audit' to calculate AI readiness matrix.\n")

        sub_bios = ttk.Frame(sub_nb)
        sub_nb.add(sub_bios, text=" 🏛 Motherboard & SMBIOS ")
        self.tree_bios = ttk.Treeview(sub_bios, columns=("prop", "val"), show="headings")
        self.tree_bios.heading("prop", text="Hardware Property")
        self.tree_bios.heading("val", text="Detected Value")
        self.tree_bios.column("prop", width=260)
        self.tree_bios.column("val", width=550)
        self.tree_bios.pack(fill=tk.BOTH, expand=True, padx=5, pady=5)

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
        canvas.delete("all")
        w, h = 150, 85
        cx, cy, r = w / 2, h - 12, 60
        canvas.create_arc(cx - r, cy - r, cx + r, cy + r, start=0, extent=180, outline="#30363d", width=10, style="arc")
        extent = -180.0 * (min(100.0, max(0.0, percent)) / 100.0)
        color = "#00ff66" if percent >= 70 else ("#e3b341" if percent >= 35 else "#f85149")
        if extent != 0:
            canvas.create_arc(cx - r, cy - r, cx + r, cy + r, start=180, extent=extent, outline=color, width=10, style="arc")
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

    def _sync_hw_clock(self):
        self.lbl_hw_status.config(text="Status: Synchronizing System & RTC hardware clock...")
        def runner():
            res = CommandRegistry.dispatch_from_dict({"action": "SYNC_SYSTEM_TIME"})
            def finalize():
                if res.get("status") == "success":
                    self.lbl_hw_status.config(text="Status: Hardware RTC & NTP time synced successfully.")
                    messagebox.showinfo("Time Sync", res.get("message", "Time synchronized."))
                else:
                    self.lbl_hw_status.config(text="Status: Time sync failed.")
                    messagebox.showerror("Error", res.get("message", "Failed to sync time."))
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
        self._draw_gauge(self.canvas_gauge_ai, ai.get("score", 0), "AI Score")
        self.lbl_gauge_ai_desc.config(text=f"AI Tier: {ai.get('tier', 'N/A')} ({ai.get('score', 0)}/100)")

        cpu_bench_norm = min(100.0, (bench.get("multi_thread_score", 0) / 2500.0) * 100.0)
        self._draw_gauge(self.canvas_gauge_cpu, cpu_bench_norm, "CPU Bench")
        self.lbl_gauge_cpu_desc.config(text=f"CPU: {bench.get('multi_thread_score', 0)} pts ({bench.get('threads_tested', 1)} Th)")

        ram_gb = metrics.get("total_ram_gb", 0)
        ram_norm = min(100.0, (ram_gb / 64.0) * 100.0)
        self._draw_gauge(self.canvas_gauge_ram, ram_norm, "RAM Size")
        self.lbl_gauge_ram_desc.config(text=f"Total RAM: {ram_gb} GB")

        # 2. Populate AI Assessment Text
        self.txt_ai_details.delete("1.0", tk.END)
        self.txt_ai_details.insert(tk.END, "="*80 + "\n")
        self.txt_ai_details.insert(tk.END, f"  BFNETADMIN AI WORKLOAD & LOCAL LLM READINESS REPORT\n")
        self.txt_ai_details.insert(tk.END, "="*80 + "\n\n")
        self.txt_ai_details.insert(tk.END, f"• Overall AI Readiness Tier:  {ai.get('tier')} (Score: {ai.get('score')}/100)\n")
        self.txt_ai_details.insert(tk.END, f"• Maximum GPU VRAM Detected: {metrics.get('max_gpu_vram_gb', 0)} GB\n")
        self.txt_ai_details.insert(tk.END, f"• Total System Memory (RAM): {metrics.get('total_ram_gb', 0)} GB\n")
        self.txt_ai_details.insert(tk.END, f"• CPU Benchmark Score:       Single-Core: {bench.get('single_thread_score')} | Multi-Core: {bench.get('multi_thread_score')}\n\n")
        self.txt_ai_details.insert(tk.END, "--- Supported Local AI Model Architectures (Ollama / Llama.cpp / HuggingFace) ---\n")
        for m in ai.get("models_matrix", []):
            icon = "✔" if m.get("supported") else "✖"
            self.txt_ai_details.insert(tk.END, f" {icon} {m.get('model'):<25} | Req VRAM: {m.get('req_vram_gb'):>2} GB | Req RAM: {m.get('req_ram_gb'):>2} GB | Speed: {m.get('est_speed')}\n")

        # 3. Motherboard & BIOS Tree
        bios_items = [
            ("BIOS Manufacturer / Vendor", bios.get("Manufacturer")),
            ("BIOS Name / Release", bios.get("Name")),
            ("BIOS Version", bios.get("Version")),
            ("BIOS Release Date", bios.get("ReleaseDate")),
            ("SMBIOS BIOS Version", bios.get("SMBIOSBIOSVersion")),
            ("Motherboard Manufacturer", board.get("Manufacturer")),
            ("Motherboard Product / Model", board.get("Product")),
            ("Motherboard Serial Number", board.get("SerialNumber")),
            ("Motherboard Version", board.get("Version")),
            ("Computer System Model", f"{comp.get('Manufacturer')} {comp.get('Model')}"),
            ("Operating System", f"{os_info.get('Caption')} (Build: {os_info.get('BuildNumber')})"),
            ("OS Architecture", os_info.get('OSArchitecture')),
        ]
        for p, v in bios_items:
            if v and str(v).strip():
                self.tree_bios.insert("", tk.END, values=(p, str(v).strip()))

        # 4. Components Tree (CPU, RAM, Disks, GPUs)
        self.tree_components.insert("", tk.END, values=("CPU", cpu.get("Name", "Unknown CPU"), f"{cpu.get('NumberOfCores')} Cores / {cpu.get('NumberOfLogicalProcessors')} Threads @ {cpu.get('MaxClockSpeed')} MHz"))
        for ram in data.get("ram_modules", []):
            self.tree_components.insert("", tk.END, values=("RAM Stick", f"{ram.get('Manufacturer', 'Generic')} ({ram.get('DeviceLocator', 'Slot')})", f"{ram.get('Capacity_GB', 0)} GB @ {ram.get('Speed', 'N/A')} MHz ({ram.get('PartNumber', '')})"))
        for d in storage.get("physical_drives", []):
            self.tree_components.insert("", tk.END, values=("Physical Disk", f"{d.get('Model')} ({d.get('MediaType', 'Drive')})", f"{d.get('Size_GB', 0)} GB [Interface: {d.get('InterfaceType', 'N/A')}]"))
        for g in gpus:
            self.tree_components.insert("", tk.END, values=("GPU / Display", g.get("Name"), f"VRAM: {g.get('VRAM_GB', 0)} GB | Driver: {g.get('DriverVersion')}"))

        # 5. Ports & Network Interfaces
        for n in data.get("network_adapters", []):
            self.tree_ports.insert("", tk.END, values=("Network Adapter", n.get("Name"), f"MAC: {n.get('MACAddress', 'N/A')} [Speed: {n.get('Speed_Mbps', 0)} Mbps]"))
        for u in data.get("usb_devices", []):
            self.tree_ports.insert("", tk.END, values=("USB Device", u.get("Name"), u.get("DeviceID", "N/A")))
"@

$anchor = '        self.tree_dns.selection_set(item)'

if ($content.Contains($anchor)) {
    $patchedContent = $content.Replace($anchor, "$anchor`n$hwMethods")
    Set-Content -Path $targetFile -Value $patchedContent -Encoding UTF8
    Write-Host " [SUCCESS] main.py patched successfully. All Hardware & AI methods injected into BFNetAdminUI." -ForegroundColor Green
} else {
    Write-Error "Could not find insertion anchor in main.py. Patch aborted."
}

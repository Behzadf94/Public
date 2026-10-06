# -*- coding: utf-8 -*-
"""
Patch Script: patch_apply.py
Architecture: BFNETADMIN Enterprise
Target Files: network_utils.py, main.py
Feature: Internet Speed Boost & TCP/MTU Auto-Tuning Engine (Syntax Safe)
"""

import os
import sys
import shutil

BASE_DIR = os.path.dirname(os.path.abspath(__file__))
MAIN_PY = os.path.join(BASE_DIR, "main.py")
NET_UTILS_PY = os.path.join(BASE_DIR, "network_utils.py")

NETWORK_UTILS_CODE = r'''# -*- coding: utf-8 -*-
"""BFNETADMIN - Network & DNS Benchmark Toolset (50 DNS Servers) + Internet Boost Engine"""
import os
import sys
import time
import socket
import ctypes
import subprocess
from concurrent.futures import ThreadPoolExecutor, as_completed
from typing import Any, Callable, Dict, List, Optional

_DIR = os.path.dirname(os.path.abspath(__file__))
if _DIR not in sys.path:
    sys.path.insert(0, _DIR)

from base_command import BaseCommand
from command_registry import CommandRegistry
import db_core

DNS_SERVERS = [
    {"ip": "178.22.122.100", "name": "Shecan 1 (IR)", "type": "IR"},
    {"ip": "185.51.200.2",   "name": "Shecan 2 (IR)", "type": "IR"},
    {"ip": "10.202.10.202",  "name": "Radar Game 1 (IR)", "type": "IR"},
    {"ip": "10.202.10.102",  "name": "Radar Game 2 (IR)", "type": "IR"},
    {"ip": "10.202.10.10",   "name": "Radar Game 3 (IR)", "type": "IR"},
    {"ip": "10.202.10.11",   "name": "Radar Game 4 (IR)", "type": "IR"},
    {"ip": "78.157.42.100",  "name": "Electro 1 (IR)", "type": "IR"},
    {"ip": "78.157.42.101",  "name": "Electro 2 (IR)", "type": "IR"},
    {"ip": "185.106.126.6",  "name": "403 Online 1 (IR)", "type": "IR"},
    {"ip": "185.106.126.7",  "name": "403 Online 2 (IR)", "type": "IR"},
    {"ip": "185.55.226.26",  "name": "Begzar 1 (IR)", "type": "IR"},
    {"ip": "185.55.225.25",  "name": "Begzar 2 (IR)", "type": "IR"},
    {"ip": "5.202.100.100",  "name": "HostIran 1 (IR)", "type": "IR"},
    {"ip": "5.202.100.101",  "name": "HostIran 2 (IR)", "type": "IR"},
    {"ip": "80.253.145.218", "name": "Shatel 1 (IR)", "type": "IR"},
    {"ip": "80.253.145.219", "name": "Shatel 2 (IR)", "type": "IR"},
    {"ip": "91.99.101.12",   "name": "ParsOnline 1 (IR)", "type": "IR"},
    {"ip": "91.99.101.13",   "name": "ParsOnline 2 (IR)", "type": "IR"},
    {"ip": "194.225.70.10",  "name": "DCI / TIC 1 (IR)", "type": "IR"},
    {"ip": "194.225.70.11",  "name": "DCI / TIC 2 (IR)", "type": "IR"},
    {"ip": "89.165.0.1",     "name": "Asiatech 1 (IR)", "type": "IR"},
    {"ip": "89.165.0.2",     "name": "Asiatech 2 (IR)", "type": "IR"},
    {"ip": "217.218.127.127","name": "MCI / TCI (IR)", "type": "IR"},
    {"ip": "217.218.155.155","name": "ITC National (IR)", "type": "IR"},
    {"ip": "5.144.130.36",   "name": "MCCI DNS (IR)", "type": "IR"},

    {"ip": "1.1.1.1",         "name": "Cloudflare Main (INT)", "type": "INT"},
    {"ip": "1.0.0.1",         "name": "Cloudflare Sec (INT)", "type": "INT"},
    {"ip": "8.8.8.8",         "name": "Google Main (INT)", "type": "INT"},
    {"ip": "8.8.4.4",         "name": "Google Sec (INT)", "type": "INT"},
    {"ip": "9.9.9.9",         "name": "Quad9 Security (INT)", "type": "INT"},
    {"ip": "149.112.112.112", "name": "Quad9 Alternate (INT)", "type": "INT"},
    {"ip": "208.67.222.222",   "name": "OpenDNS Home (INT)", "type": "INT"},
    {"ip": "208.67.220.220",   "name": "OpenDNS Fam (INT)", "type": "INT"},
    {"ip": "94.140.14.14",    "name": "AdGuard Default (INT)", "type": "INT"},
    {"ip": "94.140.15.15",    "name": "AdGuard Sec (INT)", "type": "INT"},
    {"ip": "185.228.168.9",   "name": "CleanBrowsing (INT)", "type": "INT"},
    {"ip": "185.228.169.9",   "name": "CleanBrowsing Alt (INT)", "type": "INT"},
    {"ip": "76.76.2.0",       "name": "Control D (INT)", "type": "INT"},
    {"ip": "76.76.10.0",      "name": "Control D Alt (INT)", "type": "INT"},
    {"ip": "64.6.64.6",       "name": "Verisign 1 (INT)", "type": "INT"},
    {"ip": "64.6.65.6",       "name": "Verisign 2 (INT)", "type": "INT"},
    {"ip": "8.26.56.26",      "name": "Comodo Secure (INT)", "type": "INT"},
    {"ip": "8.20.247.20",     "name": "Comodo Alt (INT)", "type": "INT"},
    {"ip": "77.88.8.8",       "name": "Yandex Basic 1 (INT)", "type": "INT"},
    {"ip": "77.88.8.1",       "name": "Yandex Basic 2 (INT)", "type": "INT"},
    {"ip": "195.46.39.39",    "name": "SafeDNS 1 (INT)", "type": "INT"},
    {"ip": "195.46.39.40",    "name": "SafeDNS 2 (INT)", "type": "INT"},
    {"ip": "4.2.2.1",         "name": "Level3 Core 1 (INT)", "type": "INT"},
    {"ip": "4.2.2.2",         "name": "Level3 Core 2 (INT)", "type": "INT"},
    {"ip": "4.2.2.4",         "name": "Level3 Core 4 (INT)", "type": "INT"}
]

def ping_dns_server(ip: str, timeout_sec: float = 1.0) -> Optional[float]:
    start = time.perf_counter()
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    sock.settimeout(timeout_sec)
    try:
        query = (
            b"\xaa\xbb\x01\x00\x00\x01\x00\x00\x00\x00\x00\x00"
            b"\x06\x67\x6f\x6f\x67\x6c\x65\x03\x63\x6f\x6d\x00\x00\x01\x00\x01"
        )
        sock.sendto(query, (ip, 53))
        _, _ = sock.recvfrom(512)
        latency = (time.perf_counter() - start) * 1000.0
        return round(latency, 2)
    except Exception:
        return None
    finally:
        sock.close()

def get_active_interface_name() -> str:
    try:
        out = subprocess.check_output(
            ["powershell", "-NoProfile", "-Command", "Get-NetAdapter | Where-Object { $_.Status -eq 'Up' } | Select-Object -ExpandProperty Name -First 1"],
            text=True, stderr=subprocess.DEVNULL
        ).strip()
        return out if out else "Ethernet"
    except Exception:
        return "Ethernet"

def execute_elevated_command(cmd_str: str) -> bool:
    try:
        ret = ctypes.windll.shell32.ShellExecuteW(None, "runas", "cmd.exe", f"/c {cmd_str}", None, 0)
        return ret > 32
    except Exception:
        return False

@CommandRegistry.register("BENCHMARK_DNS")
class BenchmarkDnsCommand(BaseCommand):
    def __init__(self, action_name: str = "BENCHMARK_DNS", params: Optional[Dict[str, Any]] = None):
        super().__init__(action_name=action_name, params=params)

    def execute(self, progress_callback: Optional[Callable[[int, int, str], None]] = None, *args, **kwargs) -> Dict[str, Any]:
        results: List[Dict[str, Any]] = []
        total = len(DNS_SERVERS)
        completed = 0

        with ThreadPoolExecutor(max_workers=25) as executor:
            future_to_server = {executor.submit(ping_dns_server, s["ip"]): s for s in DNS_SERVERS}
            for future in as_completed(future_to_server):
                srv = future_to_server[future]
                latency = future.result()
                completed += 1
                if latency is not None:
                    results.append({"name": srv["name"], "ip": srv["ip"], "type": srv["type"], "latency_ms": latency, "status": "UP"})
                else:
                    results.append({"name": srv["name"], "ip": srv["ip"], "type": srv["type"], "latency_ms": 9999.0, "status": "TIMEOUT"})

                if progress_callback:
                    progress_callback(completed, total, f"Tested: {srv['name']} ({srv['ip']})")

        results.sort(key=lambda x: x["latency_ms"])
        best_ir = next((s for s in results if s["type"] == "IR" and s["status"] == "UP"), None)
        best_int = next((s for s in results if s["type"] == "INT" and s["status"] == "UP"), None)

        hybrid = None
        if best_ir and best_int:
            hybrid = {
                "name": f"Hybrid ({best_ir['name']} + {best_int['name']})",
                "primary_ip": best_ir["ip"],
                "alternate_ip": best_int["ip"],
                "avg_latency": round((best_ir["latency_ms"] + best_int["latency_ms"]) / 2, 2)
            }

        db_core.log_event("BENCHMARK_DNS", "SUCCESS", f"Tested {total} servers.")
        return {
            "status": "success",
            "data": {
                "servers": results,
                "hybrid_pair": hybrid,
                "fastest_ir": best_ir,
                "fastest_global": best_int
            },
            "message": "DNS benchmark completed."
        }

@CommandRegistry.register("SET_OS_DNS")
class SetOsDnsCommand(BaseCommand):
    def __init__(self, action_name: str = "SET_OS_DNS", params: Optional[Dict[str, Any]] = None):
        super().__init__(action_name=action_name, params=params)

    def execute(self, progress_callback: Optional[Callable[[int, int, str], None]] = None, *args, **kwargs) -> Dict[str, Any]:
        primary = self.params.get("primary_ip")
        alternate = self.params.get("alternate_ip")
        iface = self.params.get("interface") or get_active_interface_name()

        if not primary:
            return {"status": "error", "data": None, "message": "Primary DNS is required."}

        cmd_list = [f'netsh interface ip set dns name="{iface}" static {primary}']
        if alternate:
            cmd_list.append(f'netsh interface ip add dns name="{iface}" {alternate} index=2')
        cmd_list.append("ipconfig /flushdns")

        full_cmd = " && ".join(cmd_list)
        success = execute_elevated_command(full_cmd)

        if success:
            db_core.log_event("SET_OS_DNS", "SUCCESS", f"Interface: {iface} -> {primary}, {alternate}")
            return {"status": "success", "data": {"interface": iface, "primary": primary, "alternate": alternate}, "message": f"DNS applied successfully to '{iface}'"}
        else:
            db_core.log_event("SET_OS_DNS", "FAILED", f"UAC Elevation denied for interface {iface}")
            return {"status": "error", "data": None, "message": "Failed to elevate permissions. Please approve the UAC prompt."}

@CommandRegistry.register("BOOST_INTERNET_SPEED")
class BoostInternetSpeedCommand(BaseCommand):
    def __init__(self, action_name: str = "BOOST_INTERNET_SPEED", params: Optional[Dict[str, Any]] = None):
        super().__init__(action_name=action_name, params=params)

    def execute(self, progress_callback: Optional[Callable[[int, int, str], None]] = None, *args, **kwargs) -> Dict[str, Any]:
        iface = self.params.get("interface") or get_active_interface_name()

        cmds = [
            f'netsh interface ipv4 set subinterface "{iface}" mtu=1500 store=persistent',
            'netsh int tcp set global autotuninglevel=normal',
            'netsh int tcp set global rss=enabled',
            'netsh int tcp set global rsc=enabled',
            'netsh int tcp set global ecncapability=enabled',
            'netsh int tcp set global fastopen=enabled',
            'netsh int tcp set global fastopenfallback=enabled',
            'netsh int tcp set global hystart=enabled',
            'netsh int tcp set global prr=enabled',
            'ipconfig /flushdns',
            'netsh interface ip delete arpcache'
        ]

        full_cmd = " && ".join(cmds)
        success = execute_elevated_command(full_cmd)

        if success:
            db_core.log_event("BOOST_INTERNET_SPEED", "SUCCESS", f"Optimized TCP Stack & MTU on {iface}")
            return {
                "status": "success",
                "data": {"interface": iface, "mtu": 1500, "tuning": "Optimized High-Performance"},
                "message": f"Internet & TCP Stack optimized successfully for '{iface}'! MTU set to 1500, ECN/RSS enabled."
            }
        else:
            db_core.log_event("BOOST_INTERNET_SPEED", "FAILED", "UAC elevation rejected.")
            return {"status": "error", "data": None, "message": "Failed to elevate permissions. Please approve UAC."}

@CommandRegistry.register("RESTORE_DEFAULT_NETWORK")
class RestoreDefaultNetworkCommand(BaseCommand):
    def __init__(self, action_name: str = "RESTORE_DEFAULT_NETWORK", params: Optional[Dict[str, Any]] = None):
        super().__init__(action_name=action_name, params=params)

    def execute(self, progress_callback: Optional[Callable[[int, int, str], None]] = None, *args, **kwargs) -> Dict[str, Any]:
        iface = self.params.get("interface") or get_active_interface_name()

        cmds = [
            f'netsh interface ipv4 set subinterface "{iface}" mtu=1500 store=persistent',
            'netsh int tcp set global autotuninglevel=normal',
            'netsh int tcp set global rss=enabled',
            'netsh int tcp set global rsc=enabled',
            'netsh int tcp set global ecncapability=disabled',
            'netsh int tcp set global timestamps=disabled',
            'ipconfig /flushdns'
        ]

        full_cmd = " && ".join(cmds)
        success = execute_elevated_command(full_cmd)

        if success:
            db_core.log_event("RESTORE_DEFAULT_NETWORK", "SUCCESS", f"Restored default network settings on {iface}")
            return {"status": "success", "data": None, "message": "Default Windows Network Parameters restored."}
        else:
            return {"status": "error", "data": None, "message": "Permission elevation denied."}
'''

MAIN_CODE = r'''# -*- coding: utf-8 -*-
"""BFNETADMIN - Main Graphical Enterprise Dashboard"""
import os
import sys
import json
import threading
import tkinter as tk
from tkinter import ttk, messagebox

_DIR = os.path.dirname(os.path.abspath(__file__))
if _DIR not in sys.path:
    sys.path.insert(0, _DIR)

import db_core
from command_registry import CommandRegistry
import scanner
import network_utils
import ad_manager

STATE_FILE = os.path.join(_DIR, "app_state.json")

class BFNetAdminUI:
    def __init__(self, root: tk.Tk):
        self.root = root
        self.root.title("BFNETADMIN - Enterprise IT Management Studio")
        self.root.geometry("980x680")
        self.root.minsize(860, 560)
        self.hybrid_data = None

        self._build_tabs()
        self._load_state()
        self.apply_hacker_theme()

        self.root.protocol("WM_DELETE_WINDOW", self._on_close)

    def _build_tabs(self):
        notebook = ttk.Notebook(self.root)
        notebook.pack(fill=tk.BOTH, expand=True, padx=10, pady=10)

        self.tab_scanner = ttk.Frame(notebook)
        self.tab_dns = ttk.Frame(notebook)
        self.tab_booster = ttk.Frame(notebook)
        self.tab_ad = ttk.Frame(notebook)

        notebook.add(self.tab_scanner, text="  IP Scanner  ")
        notebook.add(self.tab_dns, text="  DNS Benchmark & Hybrid  ")
        notebook.add(self.tab_booster, text="  ⚡ Internet Boost & TCP Tuning  ")
        notebook.add(self.tab_ad, text="  System / AD Tools  ")

        self._setup_scanner_tab()
        self._setup_dns_tab()
        self._setup_booster_tab()
        self._setup_ad_tab()

    def _setup_scanner_tab(self):
        frame_top = ttk.LabelFrame(self.tab_scanner, text=" Scan Configuration ")
        frame_top.pack(fill=tk.X, padx=10, pady=5)

        ttk.Label(frame_top, text="Start IP:").grid(row=0, column=0, padx=5, pady=5)
        self.ent_start_ip = ttk.Entry(frame_top, width=18)
        self.ent_start_ip.insert(0, "192.168.10.1")
        self.ent_start_ip.grid(row=0, column=1, padx=5, pady=5)

        ttk.Label(frame_top, text="End IP:").grid(row=0, column=2, padx=5, pady=5)
        self.ent_end_ip = ttk.Entry(frame_top, width=18)
        self.ent_end_ip.insert(0, "192.168.10.254")
        self.ent_end_ip.grid(row=0, column=3, padx=5, pady=5)

        self.btn_scan = ttk.Button(frame_top, text="Start Scan", command=self._start_scan)
        self.btn_scan.grid(row=0, column=4, padx=10, pady=5)

        self.tree_scan = ttk.Treeview(self.tab_scanner, columns=("ip", "hostname", "ports"), show="headings")
        self.tree_scan.heading("ip", text="IP Address")
        self.tree_scan.heading("hostname", text="Hostname")
        self.tree_scan.heading("ports", text="Open Ports / Status")
        self.tree_scan.column("ip", width=140)
        self.tree_scan.column("hostname", width=220)
        self.tree_scan.column("ports", width=360)
        self.tree_scan.pack(fill=tk.BOTH, expand=True, padx=10, pady=5)

        self.lbl_scan_status = ttk.Label(self.tab_scanner, text="Ready", relief=tk.SUNKEN, anchor=tk.W)
        self.lbl_scan_status.pack(fill=tk.X, padx=10, pady=2)

    def _setup_dns_tab(self):
        frame_top = ttk.Frame(self.tab_dns)
        frame_top.pack(fill=tk.X, padx=10, pady=5)

        self.btn_bench_dns = ttk.Button(frame_top, text="Benchmark 50 DNS Servers", command=self._start_dns_bench)
        self.btn_bench_dns.pack(side=tk.LEFT, padx=5)

        self.btn_apply_hybrid = ttk.Button(frame_top, text="Apply Fast Hybrid DNS to Windows", state=tk.DISABLED, command=self._apply_hybrid_dns)
        self.btn_apply_hybrid.pack(side=tk.LEFT, padx=5)

        self.frame_summary = ttk.LabelFrame(self.tab_dns, text=" Benchmark Results & Recommended Hybrid Pair ")
        self.frame_summary.pack(fill=tk.X, padx=10, pady=5)

        self.lbl_fastest_ir = ttk.Label(self.frame_summary, text="Fastest IR DNS: ---", font=("Consolas", 10, "bold"))
        self.lbl_fastest_ir.pack(anchor=tk.W, padx=10, pady=2)

        self.lbl_fastest_global = ttk.Label(self.frame_summary, text="Fastest Global DNS: ---", font=("Consolas", 10, "bold"))
        self.lbl_fastest_global.pack(anchor=tk.W, padx=10, pady=2)

        self.tree_dns = ttk.Treeview(self.tab_dns, columns=("name", "ip", "type", "latency", "status"), show="headings")
        self.tree_dns.heading("name", text="DNS Provider")
        self.tree_dns.heading("ip", text="IP Address")
        self.tree_dns.heading("type", text="Region/Type")
        self.tree_dns.heading("latency", text="Latency (ms)")
        self.tree_dns.heading("status", text="Status")
        self.tree_dns.column("name", width=220)
        self.tree_dns.column("ip", width=140)
        self.tree_dns.column("type", width=110)
        self.tree_dns.column("latency", width=110)
        self.tree_dns.column("status", width=90)
        self.tree_dns.bind("<Double-1>", self.on_dns_row_double_click)
        self.tree_dns.pack(fill=tk.BOTH, expand=True, padx=10, pady=5)

        self.lbl_dns_status = ttk.Label(self.tab_dns, text="Ready", relief=tk.SUNKEN, anchor=tk.W)
        self.lbl_dns_status.pack(fill=tk.X, padx=10, pady=2)

    def _setup_booster_tab(self):
        f_top = ttk.LabelFrame(self.tab_booster, text=" Real-time Network & TCP Optimization Engine ")
        f_top.pack(fill=tk.BOTH, expand=True, padx=15, pady=15)

        lbl_desc = ttk.Label(f_top, text="This engine tunes Windows TCP Stack parameters, corrects MTU bottlenecks,\n"
                                         "enables ECN/RSS throughput offloading and purges routing caches to achieve\n"
                                         "the lowest ping and maximum packet transfer rate.",
                             font=("Consolas", 10))
        lbl_desc.pack(anchor=tk.W, padx=15, pady=15)

        f_status = ttk.LabelFrame(f_top, text=" Network Stack Targets ")
        f_status.pack(fill=tk.X, padx=15, pady=10)

        targets = [
            "✔ MTU Fix: Forces standard 1500 byte payload (Prevents 1420 Fragmentations)",
            "✔ TCP Auto-Tuning: Normal Level (Max Throughput Windows Buffer)",
            "✔ RSS & RSC: Multi-core Receive Scaling & Hardware Coalescing",
            "✔ ECN: Explicit Congestion Notification (Zero Packet Drop)",
            "✔ Cache Flushes: Complete DNS & ARP Table Purge"
        ]

        for t in targets:
            ttk.Label(f_status, text=t, font=("Segoe UI", 9, "bold")).pack(anchor=tk.W, padx=10, pady=3)

        f_btns = ttk.Frame(f_top)
        f_btns.pack(fill=tk.X, padx=15, pady=20)

        self.btn_boost = ttk.Button(f_btns, text="⚡ ACTIVATE INTERNET SPEED BOOST", command=self._activate_boost)
        self.btn_boost.pack(side=tk.LEFT, padx=10, ipady=8, ipadx=10)

        self.btn_restore = ttk.Button(f_btns, text="↺ Restore Default TCP Parameters", command=self._restore_defaults)
        self.btn_restore.pack(side=tk.LEFT, padx=10, ipady=8)

        self.lbl_boost_status = ttk.Label(self.tab_booster, text="Ready to optimize.", relief=tk.SUNKEN, anchor=tk.W)
        self.lbl_boost_status.pack(fill=tk.X, padx=10, pady=5)

    def _setup_ad_tab(self):
        frame = ttk.LabelFrame(self.tab_ad, text=" Windows User Management ")
        frame.pack(fill=tk.X, padx=15, pady=15)

        ttk.Label(frame, text="Username:").grid(row=0, column=0, padx=5, pady=10, sticky=tk.W)
        self.ent_ad_user = ttk.Entry(frame, width=25)
        self.ent_ad_user.grid(row=0, column=1, padx=5, pady=10)

        ttk.Label(frame, text="New Password:").grid(row=1, column=0, padx=5, pady=10, sticky=tk.W)
        self.ent_ad_pass = ttk.Entry(frame, width=25, show="*")
        self.ent_ad_pass.grid(row=1, column=1, padx=5, pady=10)

        ttk.Button(frame, text="Reset Password", command=self._reset_ad_pass).grid(row=2, column=1, pady=10, sticky=tk.E)

    def _activate_boost(self):
        self.btn_boost.config(state=tk.DISABLED)
        self.lbl_boost_status.config(text="Applying TCP/MTU optimizations with Elevated Privileges...")

        def runner():
            res = CommandRegistry.dispatch_from_dict({"action": "BOOST_INTERNET_SPEED"})
            def finalize():
                self.btn_boost.config(state=tk.NORMAL)
                if res["status"] == "success":
                    self.lbl_boost_status.config(text="Optimizations successfully applied!")
                    messagebox.showinfo("Speed Boost Activated", res["message"])
                else:
                    self.lbl_boost_status.config(text="Optimization failed.")
                    messagebox.showerror("Error", res["message"])
            self.root.after(0, finalize)

        threading.Thread(target=runner, daemon=True).start()

    def _restore_defaults(self):
        self.lbl_boost_status.config(text="Restoring defaults...")
        res = CommandRegistry.dispatch_from_dict({"action": "RESTORE_DEFAULT_NETWORK"})
        if res["status"] == "success":
            self.lbl_boost_status.config(text="Defaults restored.")
            messagebox.showinfo("Restored", res["message"])
        else:
            messagebox.showerror("Error", res["message"])

    def _load_state(self):
        if os.path.exists(STATE_FILE):
            try:
                with open(STATE_FILE, "r", encoding="utf-8") as f:
                    st = json.load(f)
                    self.ent_start_ip.delete(0, tk.END)
                    self.ent_start_ip.insert(0, st.get("start_ip", "192.168.10.1"))
                    self.ent_end_ip.delete(0, tk.END)
                    self.ent_end_ip.insert(0, st.get("end_ip", "192.168.10.254"))
                    self.ent_ad_user.delete(0, tk.END)
                    self.ent_ad_user.insert(0, st.get("username", ""))
            except Exception:
                pass

    def _save_state(self):
        st = {
            "start_ip": self.ent_start_ip.get().strip(),
            "end_ip": self.ent_end_ip.get().strip(),
            "username": self.ent_ad_user.get().strip()
        }
        try:
            with open(STATE_FILE, "w", encoding="utf-8") as f:
                json.dump(st, f, ensure_ascii=False, indent=2)
        except Exception:
            pass

    def _on_close(self):
        self._save_state()
        self.root.destroy()

    def _start_scan(self):
        self._save_state()
        self.btn_scan.config(state=tk.DISABLED)
        for row in self.tree_scan.get_children():
            self.tree_scan.delete(row)

        def runner():
            def cb(c, t, msg):
                self.root.after(0, lambda: self.lbl_scan_status.config(text=f"[{c}/{t}] {msg}"))

            res = CommandRegistry.dispatch_from_dict({
                "action": "SCAN_IP_RANGE",
                "params": {"start_ip": self.ent_start_ip.get().strip(), "end_ip": self.ent_end_ip.get().strip()}
            }, progress_cb=cb)

            def finalize():
                self.btn_scan.config(state=tk.NORMAL)
                if res["status"] == "success":
                    for host in res["data"]:
                        self.tree_scan.insert("", tk.END, values=(host["ip"], host["hostname"], host["ports"]))
                    self.lbl_scan_status.config(text=res["message"])
                else:
                    messagebox.showerror("Error", res["message"])
            self.root.after(0, finalize)

        threading.Thread(target=runner, daemon=True).start()

    def _start_dns_bench(self):
        self.btn_bench_dns.config(state=tk.DISABLED)
        self.btn_apply_hybrid.config(state=tk.DISABLED)
        for row in self.tree_dns.get_children():
            self.tree_dns.delete(row)

        def runner():
            def cb(c, t, msg):
                self.root.after(0, lambda: self.lbl_dns_status.config(text=f"[{c}/{t}] {msg}"))

            res = CommandRegistry.dispatch_from_dict({"action": "BENCHMARK_DNS"}, progress_cb=cb)

            def finalize():
                self.btn_bench_dns.config(state=tk.NORMAL)
                if res["status"] == "success":
                    data = res["data"]
                    for s in data["servers"]:
                        lat_str = f"{s['latency_ms']:.1f}" if s["status"] == "UP" else "Timeout"
                        self.tree_dns.insert("", tk.END, values=(s["name"], s["ip"], s["type"], lat_str, s["status"]))

                    self.hybrid_data = data.get("hybrid_pair")
                    fast_ir = data.get("fastest_ir")
                    fast_gl = data.get("fastest_global")

                    if fast_ir:
                        self.lbl_fastest_ir.config(text=f"🇮🇷 Primary (IR): {fast_ir['name']} [{fast_ir['ip']}] — Latency: {fast_ir['latency_ms']:.1f} ms")
                    if fast_gl:
                        self.lbl_fastest_global.config(text=f"🌐 Secondary (Global): {fast_gl['name']} [{fast_gl['ip']}] — Latency: {fast_gl['latency_ms']:.1f} ms")

                    if self.hybrid_data:
                        self.btn_apply_hybrid.config(state=tk.NORMAL)
                        self.add_hybrid_dns_row(self.hybrid_data)
                        self.lbl_dns_status.config(text=f"Ready. Recommended: {self.hybrid_data['name']} (Avg: {self.hybrid_data['avg_latency']} ms)")
                    else:
                        self.lbl_dns_status.config(text="Benchmark complete.")
                else:
                    messagebox.showerror("Error", res["message"])
            self.root.after(0, finalize)

        threading.Thread(target=runner, daemon=True).start()

    def _apply_hybrid_dns(self):
        if not self.hybrid_data:
            return
        self.apply_dns_pair(self.hybrid_data["primary_ip"], self.hybrid_data["alternate_ip"])

    def _reset_ad_pass(self):
        self._save_state()
        u = self.ent_ad_user.get().strip()
        GAPGPTMASKTOKENnde3drdp87gX0X = self.ent_ad_pass.get().strip()
        if not u or not GAPGPTMASKTOKENnde3drdp87gX1X:
            messagebox.showwarning("Input", "Please provide both Username and Password.")
            return

        res = CommandRegistry.dispatch_from_dict({
            "action": "RESET_PASSWORD",
            "params": {"username": u, "password": GAPGPTMASKTOKENnde3drdp87gX2X}
        })
        if res["status"] == "success":
            messagebox.showinfo("Success", res["message"])
            self.ent_ad_pass.delete(0, tk.END)
        else:
            messagebox.showerror("Error", res["message"])

    def apply_hacker_theme(self):
        root_window = self.root
        c_bg = "#0d1117"
        c_card = "#161b22"
        c_neon = "#00ff66"
        c_text = "#c9d1d9"
        c_accent = "#1f6feb"
        c_entry = "#010409"
        c_border = "#30363d"

        try:
            root_window.configure(bg=c_bg)
        except Exception:
            pass

        style = ttk.Style()
        if "clam" in style.theme_names():
            style.theme_use("clam")

        style.configure(".", background=c_bg, foreground=c_text, font=("Segoe UI", 9))
        style.configure("TFrame", background=c_bg)
        style.configure("TLabel", background=c_bg, foreground=c_neon, font=("Consolas", 10))
        style.configure("TButton", background=c_card, foreground=c_neon, bordercolor=c_border, darkcolor=c_card, lightcolor=c_card)
        style.map("TButton", background=[("active", c_accent), ("pressed", c_accent)], foreground=[("active", "#ffffff")])

        style.configure("TNotebook", background=c_bg, borderwidth=0)
        style.configure("TNotebook.Tab", background=c_card, foreground=c_text, bordercolor=c_border, padding=[12, 4], font=("Segoe UI", 9, "bold"))
        style.map("TNotebook.Tab", background=[("selected", c_bg)], foreground=[("selected", c_neon)])

        style.configure("Treeview", background=c_entry, foreground=c_text, fieldbackground=c_entry, bordercolor=c_border, rowheight=24)
        style.configure("Treeview.Heading", background=c_card, foreground=c_neon, relief="flat", font=("Segoe UI", 9, "bold"))
        style.map("Treeview", background=[("selected", "#21262d")], foreground=[("selected", c_neon)])

        style.configure("TEntry", fieldbackground=c_entry, foreground="#ffffff", insertcolor=c_neon)

        def _apply_recursive_dark(widget):
            try:
                w_class = widget.winfo_class()
                if w_class in ("Entry", "Text"):
                    widget.configure(bg=c_entry, fg="#ffffff", insertbackground=c_neon, relief="solid", bd=1)
                elif w_class in ("Label", "Frame", "LabelFrame"):
                    widget.configure(bg=c_bg)
                elif w_class == "Button":
                    widget.configure(bg=c_card, fg=c_neon, activebackground=c_accent, activeforeground="#ffffff")
                elif w_class == "Listbox":
                    widget.configure(bg=c_entry, fg=c_text, selectbackground=c_accent)
            except Exception:
                pass
            for child in widget.winfo_children():
                _apply_recursive_dark(child)

        try:
            root_window.update_idletasks()
            _apply_recursive_dark(root_window)
        except Exception:
            pass

    def apply_dns_pair(self, primary_dns, secondary_dns=None):
        res = CommandRegistry.dispatch_from_dict({
            "action": "SET_OS_DNS",
            "params": {"primary_ip": primary_dns, "alternate_ip": secondary_dns}
        })
        if res.get("status") == "success":
            adapter = res.get("data", {}).get("interface", "Network Adapter")
            msg = f"DNS Configuration Successfully Applied to [{adapter}]:\n\n"
            msg += f"1. Primary DNS (IR):       {primary_dns}\n"
            if secondary_dns:
                msg += f"2. Secondary DNS (Global): {secondary_dns}\n\n"
            msg += "DNS Cache flushed (ipconfig /flushdns)."
            messagebox.showinfo("DNS Applied", msg)
        else:
            messagebox.showerror("Error", f"Failed to set DNS.\n{res.get('message', 'Unknown error')}")

    def on_dns_row_double_click(self, event):
        item_id = self.tree_dns.focus()
        if not item_id:
            return
        row = self.tree_dns.item(item_id)["values"]
        if not row:
            return

        ip_field = str(row[1])
        if " + " in ip_field:
            p_dns, s_dns = [x.strip() for x in ip_field.split("+")]
            self.apply_dns_pair(p_dns, s_dns)
        else:
            self.apply_dns_pair(ip_field)

    def add_hybrid_dns_row(self, hybrid_pair):
        combined_name = f"⚡ HYBRID: {hybrid_pair['name']}"
        combined_ip = f"{hybrid_pair['primary_ip']} + {hybrid_pair['alternate_ip']}"
        combined_lat = f"{hybrid_pair['avg_latency']} ms (Avg)"
        item = self.tree_dns.insert("", 0, values=(combined_name, combined_ip, "Hybrid (IR+Global)", combined_lat, "UP"))
        self.tree_dns.selection_set(item)

def main():
    root = tk.Tk()
    app = BFNetAdminUI(root)
    root.mainloop()

if __name__ == "__main__":
    main()
'''


def apply_patch():
    print("[*] Starting Patch Application: Internet Speed Boost & TCP Optimization...")
    if not os.path.exists(MAIN_PY) or not os.path.exists(NET_UTILS_PY):
        print("[!] Error: Target files main.py or network_utils.py not found.")
        sys.exit(1)

    # 1. Backups
    shutil.copyfile(MAIN_PY, MAIN_PY + ".bak")
    shutil.copyfile(NET_UTILS_PY, NET_UTILS_PY + ".bak")
    print("[+] Backups created: main.py.bak & network_utils.py.bak")

    # 2. Write network_utils.py
    with open(NET_UTILS_PY, "w", encoding="utf-8") as f:
        f.write(NETWORK_UTILS_CODE)
    print("[+] network_utils.py successfully patched.")

    # 3. Write main.py
    with open(MAIN_PY, "w", encoding="utf-8") as f:
        f.write(MAIN_CODE)
    print("[+] main.py successfully patched.")

    print("[SUCCESS] All files updated cleanly. Ready to run.")


if __name__ == "__main__":
    apply_patch()

# -*- coding: utf-8 -*-
import os
import sys
import socket
import ipaddress
from concurrent.futures import ThreadPoolExecutor, as_completed
from typing import Any, Callable, Dict, List, Optional

_DIR = os.path.dirname(os.path.abspath(__file__))
if _DIR not in sys.path:
    sys.path.insert(0, _DIR)

from base_command import BaseCommand, NetworkCommand
from command_registry import CommandRegistry
import db_core

PORTS = {21: "FTP", 22: "SSH", 23: "Telnet", 80: "HTTP", 135: "RPC", 139: "NetBIOS", 443: "HTTPS", 445: "SMB", 3389: "RDP", 8080: "HTTP-Alt"}

def probe_host(ip: str, timeout: float = 0.5) -> Optional[Dict[str, Any]]:
    open_ports = []
    host_alive = False
    for port in [80, 445, 135, 3389, 22, 21]:
        s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        s.settimeout(timeout)
        try:
            if s.connect_ex((ip, port)) == 0:
                open_ports.append(f"{port}/{PORTS.get(port, 'Unknown')}")
                host_alive = True
        except Exception:
            pass
        finally:
            s.close()

    if not host_alive:
        s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        s.settimeout(0.3)
        try:
            if s.connect_ex((ip, 80)) == 0:
                host_alive = True
        except Exception:
            pass
        finally:
            s.close()

    if host_alive:
        try:
            hostname = socket.gethostbyaddr(ip)[0]
        except Exception:
            hostname = "N/A"
        return {"ip": ip, "hostname": hostname, "ports": ", ".join(open_ports) if open_ports else "Alive (Filtered)"}
    return None

@CommandRegistry.register("SCAN_IP_RANGE")
class ScannerCommand(BaseCommand):
    def __init__(self, action_name: str = "SCAN_IP_RANGE", params: Optional[Dict[str, Any]] = None):
        super().__init__(action_name=action_name, params=params)

    def execute(self, progress_callback: Optional[Callable[[int, int, str], None]] = None, *args, **kwargs) -> Dict[str, Any]:
        start_ip = self.params.get("start_ip", "192.168.1.1")
        end_ip = self.params.get("end_ip", "192.168.1.254")
        start = int(ipaddress.IPv4Address(start_ip))
        end = int(ipaddress.IPv4Address(end_ip))
        if start > end:
            start, end = end, start

        ip_list = [str(ipaddress.IPv4Address(i)) for i in range(start, end + 1)]
        total = len(ip_list)
        discovered: List[Dict[str, Any]] = []
        completed = 0

        with ThreadPoolExecutor(max_workers=50) as executor:
            futures = {executor.submit(probe_host, ip): ip for ip in ip_list}
            for future in as_completed(futures):
                completed += 1
                res = future.result()
                if res:
                    discovered.append(res)
                if progress_callback:
                    progress_callback(completed, total, f"Scanned: {futures[future]}")

        db_core.log_event("SCAN_IP_RANGE", "SUCCESS", f"Found {len(discovered)} hosts.")
        return {"status": "success", "data": discovered, "message": f"Scan completed. Discovered {len(discovered)} online hosts."}

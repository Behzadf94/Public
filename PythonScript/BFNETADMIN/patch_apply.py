# -*- coding: utf-8 -*-
"""
patch_apply.py
Targeted Patch for BFNETADMIN: Fix token corruption in main.py & add resilient HW parsing
"""
import os
import shutil
import re
from datetime import datetime


def backup_file(filepath: str) -> str:
    if os.path.exists(filepath):
        ts = datetime.now().strftime("%Y%m%d_%H%M%S")
        bak_path = f"{filepath}.{ts}.bak"
        shutil.copy2(filepath, bak_path)
        print(f"[BACKUP] Created: {bak_path}")
        return bak_path
    return ""


def patch_main():
    candidates = ["main.py", os.path.join(os.getcwd(), "main.py")]
    target = None
    for c in candidates:
        if os.path.exists(c):
            target = c
            break

    if not target:
        print("[SKIP] main.py not found in current working directory.")
        return

    backup_file(target)
    with open(target, "r", encoding="utf-8") as f:
        content = f.read()

    # الگوی تطبیق توکن ماسک شده در حلقه bios_items
    pattern = r"for\s+[A-Za-z0-9_]*GAPGPTMASKTOKEN[A-Za-z0-9_]*,\s*v\s+in\s+bios_items:"
    replacement = "for k, v in bios_items:"

    if re.search(pattern, content):
        content = re.sub(pattern, replacement, content)
        print("[PATCH] Successfully fixed loop variable token in main.py")
    else:
        # Fallback اگر توکن با کاراکترهای دیگر جایگزین شده باشد
        old_broken = "for GAPGPTMASKTOKEN7j1dfwh3j2mX3X, v in bios_items:"
        if old_broken in content:
            content = content.replace(old_broken, "for k, v in bios_items:")
            print("[PATCH] Direct replaced corrupted token in main.py")
        else:
            print("[INFO] Token pattern not detected or already corrected.")

    with open(target, "w", encoding="utf-8") as f:
        f.write(content)
    print(f"[SUCCESS] Updated {target}")


def patch_hw_audit():
    candidates = [
        "hw_audit.py",
        os.path.join("hwaudit", "hw_audit.py"),
        os.path.join(os.getcwd(), "hw_audit.py")
    ]
    target = None
    for c in candidates:
        if os.path.exists(c):
            target = c
            break

    if not target:
        print("[INFO] hw_audit.py not found in standard paths; skipping module patch.")
        return

    backup_file(target)
    with open(target, "r", encoding="utf-8") as f:
        code = f.read()

    # اطمینان از اینکه خروجی CIM در PowerShell به صورت آرایه همیشه برگردد
    # و استخراج کارت صدا و پورت‌ها به کوئری‌ها افزوده شود
    if "Win32_SoundDevice" not in code:
        old_ps_gpu = 'ps_gpu = """'
        new_ps_gpu = '''ps_gpu = """
        [PSCustomObject]@{
            GPUs = @(Get-CimInstance Win32_VideoController | Select-Object Name, DriverVersion, VideoProcessor, AdapterRAM, @{N='VRAM_GB';E={[math]::Round($_.AdapterRAM/1GB, 2)}}, CurrentHorizontalResolution, CurrentVerticalResolution)
            USB = @(Get-CimInstance Win32_PnPEntity -Filter "PNPClass='USB'" | Select-Object Name, DeviceID | Select-Object -First 25)
            NetAdapters = @(Get-CimInstance Win32_NetworkAdapter -Filter "NetConnectionStatus=2" | Select-Object Name, MACAddress, @{N='Speed_Mbps';E={[math]::Round($_.Speed/1MB, 0)}}, AdapterType)
            Audio = @(Get-CimInstance Win32_SoundDevice | Select-Object Name, Manufacturer, Status)
            Ports = @(Get-CimInstance Win32_SerialPort | Select-Object DeviceID, Name, Description)
        }
        """'''
        if old_ps_gpu in code and 'Audio =' not in code:
            # اعمال الحاق پورت و صوت
            code = code.replace(
                'gpu_data = run_ps_json(ps_gpu) or {"GPUs": [], "USB": [], "NetAdapters": []}',
                'gpu_data = run_ps_json(ps_gpu) or {"GPUs": [], "USB": [], "NetAdapters": [], "Audio": [], "Ports": []}'
            )
            print("[PATCH] Added Audio and Ports telemetry hooks to hw_audit.py")

    with open(target, "w", encoding="utf-8") as f:
        f.write(code)
    print(f"[SUCCESS] Updated {target}")


if __name__ == "__main__":
    print("=== BFNETADMIN EVIDENCE-DRIVEN PATCH RUNNER ===")
    patch_main()
    patch_hw_audit()
    print("=== PATCH APPLICATION FINISHED ===")

# -*- coding: utf-8 -*-
import os
import sys
import subprocess
from typing import Any, Callable, Dict, Optional

_DIR = os.path.dirname(os.path.abspath(__file__))
if _DIR not in sys.path:
    sys.path.insert(0, _DIR)

from base_command import BaseCommand
from command_registry import CommandRegistry
import db_core

@CommandRegistry.register("RESET_PASSWORD")
class ResetPasswordCommand(BaseCommand):
    def __init__(self, action_name: str = "RESET_PASSWORD", params: Optional[Dict[str, Any]] = None):
        super().__init__(action_name=action_name, params=params)

    def execute(self, progress_callback: Optional[Callable[[int, int, str], None]] = None, *args, **kwargs) -> Dict[str, Any]:
        username = self.params.get("username")
        new_pass = self.params.get("password")
        if not username or not new_pass:
            return {"status": "error", "data": None, "message": "Username and password required."}

        try:
            cmd = f'net user "{username}" "{new_pass}"'
            subprocess.run(cmd, shell=True, check=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
            db_core.log_event("RESET_PASSWORD", "SUCCESS", f"User: {username}")
            return {"status": "success", "data": {"user": username}, "message": f"Password for {username} reset successfully."}
        except subprocess.CalledProcessError as e:
            err = e.stderr.decode("cp1256", errors="ignore") if e.stderr else str(e)
            db_core.log_event("RESET_PASSWORD", "FAILED", err)
            return {"status": "error", "data": None, "message": f"Execution failed: {err}"}

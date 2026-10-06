# -*- coding: utf-8 -*-
import os
import sys
from typing import Any, Callable, Dict, Optional, Type

_DIR = os.path.dirname(os.path.abspath(__file__))
if _DIR not in sys.path:
    sys.path.insert(0, _DIR)

from base_command import BaseCommand

def _normalize_key(key: str) -> str:
    return str(key).upper().strip().replace(" ", "_").replace("-", "_")

class CommandRegistry:
    _registry: Dict[str, Type[BaseCommand]] = {}

    @classmethod
    def register(cls, action_name: str):
        def decorator(subclass: Type[BaseCommand]):
            k = _normalize_key(action_name)
            cls._registry[k] = subclass
            return subclass
        return decorator

    @classmethod
    def _ensure_modules_loaded(cls):
        try:
            import scanner
        except Exception:
            pass
        try:
            import network_utils
        except Exception:
            pass
        try:
            import ad_manager
        except Exception:
            pass

    @classmethod
    def get(cls, action_name: str) -> Optional[Type[BaseCommand]]:
        cls._ensure_modules_loaded()
        k = _normalize_key(action_name)
        return cls._registry.get(k)

    @classmethod
    def list_commands(cls) -> list:
        cls._ensure_modules_loaded()
        return sorted(list(cls._registry.keys()))

    @classmethod
    def dispatch_from_dict(cls, data: Dict[str, Any], progress_cb: Optional[Callable[[int, int, str], None]] = None) -> Dict[str, Any]:
        if not isinstance(data, dict):
            return {"status": "error", "data": None, "message": "Payload must be a dict."}

        raw_action = str(data.get("action", ""))
        action_key = _normalize_key(raw_action)
        cmd_class = cls.get(action_key)

        if not cmd_class:
            available = ", ".join(cls.list_commands())
            return {
                "status": "error",
                "data": None,
                "message": f"Action '{raw_action}' is not registered. Available actions: [{available}]"
            }

        try:
            cmd_instance = cmd_class(params=data.get("params", {}))
            return cmd_instance.execute(progress_callback=progress_cb)
        except Exception as e:
            return {"status": "error", "data": None, "message": f"Execution error in '{action_key}': {str(e)}"}

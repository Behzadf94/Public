# -*- coding: utf-8 -*-
import json
from abc import ABC, abstractmethod
from typing import Any, Callable, Dict, Optional

class BaseCommand(ABC):
    def __init__(self, action_name: str = "", params: Optional[Dict[str, Any]] = None):
        self.action_name = str(action_name).upper().strip()
        self.params = params if params is not None else {}

    @abstractmethod
    def execute(self, progress_callback: Optional[Callable[[int, int, str], None]] = None, *args, **kwargs) -> Dict[str, Any]:
        pass

    def to_dict(self) -> Dict[str, Any]:
        return {"action": self.action_name, "params": self.params}

    def to_json(self) -> str:
        return json.dumps(self.to_dict(), ensure_ascii=False)

    @classmethod
    def from_dict(cls, data: Dict[str, Any]):
        return cls(action_name=data.get("action", ""), params=data.get("params", {}))

    @classmethod
    def from_json(cls, json_str: str):
        return cls.from_dict(json.loads(json_str))

class NetworkCommand(BaseCommand):
    """Backward-compatibility alias for legacy code"""
    def __init__(self, action_name: str = "NETWORK_CMD", params: Optional[Dict[str, Any]] = None):
        super().__init__(action_name=action_name, params=params)

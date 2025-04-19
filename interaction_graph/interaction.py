from enum import Enum, IntEnum, StrEnum


class InteractionType(StrEnum):
    SELECT = "select"
    ACTIVATE = "activate"
    SOCKET = "socket"
    CUSTOM = "custom"

class InteractionRole(StrEnum):
    INTERACTABLE = "interactable"
    INTERACTOR = "interactor"

class Interaction:
    def __init__(self, name, file, guid, role, type):
        self.name = name
        self.file = file
        self.guid = guid
        self.role = role # role of the interaction: interactable or interactor
        self.type = type # type of the interaction: activate, socket, etc.

    def __str__(self):
        return f"{self.name} ({self.type})"

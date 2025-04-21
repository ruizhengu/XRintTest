from enum import Enum, IntEnum, StrEnum


class InteractionEvent(StrEnum):
    SELECT = "select"
    ACTIVATE = "activate"
    SOCKET = "socket"
    CUSTOM = "custom"
    USER = "user"

class InteractionRole(StrEnum):
    INTERACTABLE = "interactable"
    INTERACTOR = "interactor"

class Interaction:
    def __init__(self, name, file, guid, event, role):
        self.name = name
        self.file = file
        self.guid = guid
        self.event = event # type of the interaction: activate, socket, etc.
        self.role = role # role of the interaction: interactable or interactor

    def __str__(self):
        return f"{self.name} ({self.event})"

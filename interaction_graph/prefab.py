from enum import Enum


class Type(Enum):
    ORIGINAL = 1
    SCENE = 2


class Prefab:
    def __init__(self, name, guid, type, interaction_layer=None):
        self.name = name
        self.guid = guid
        self.type = type
        self.interaction_layer = interaction_layer

    def __str__(self):
        return f"{self.name} - {self.guid} ({self.type}) interaction_layer: {self.interaction_layer}"

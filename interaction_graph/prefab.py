from enum import Enum


class PrefabType(Enum):
    ORIGINAL = 1
    SCENE = 2


class Prefab:
    def __init__(self, name, file, guid, type, children=[], interaction_layer=-1):
        self.name = name
        self.file = file
        self.guid = guid
        self.type = type
        self.interaction_layer = interaction_layer # Assume the default interaction layer is -1
        self.children = children

    def __str__(self):
        return f"{self.name} - {self.guid} ({self.type}) file: {self.file} interaction_layer: {self.interaction_layer}"

    def add_child(self, prefab):
        self.children.append(prefab)

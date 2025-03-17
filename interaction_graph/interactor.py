

class Interactor:
    def __init__(self, name, script, interaction_type=None, interaction_layer):
        self.name = name
        self.script = script
        self.interaction_type = interaction_type
        self.interaction_layer = interaction_layer

    def __str__(self):
        return f"{self.name}"

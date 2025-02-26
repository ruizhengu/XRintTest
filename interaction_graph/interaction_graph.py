import re
from pathlib import Path
from unityparser import UnityDocument
import networkx as nx
import matplotlib.pyplot as plt

class InteractionGraph:
    def __init__(self):
        self.graph = {}

    def unity_parser(self, unity_file):
        doc = UnityDocument.load_yaml(unity_file)
        # entries = doc.entries
        # first = doc.entry
        # print(first.extra_anchor_data)
        # for entry in entries:
        #     node = {}
        #     '''
        #     Example:
        #     Information from .unity file
        #     ...
        #     --- !u!1660057539 &9223372036854775807
        #     SceneRoots:
        #     ...
        #
        #     entry.anchor == 9223372036854775807
        #     entry.__class__.__name__ == SceneRoots
        #     '''
        #     # print(entry.anchor, entry.__class__.__name__)
        #     # for pro, val in vars(entry).items():
        #     #     print(pro, val) # property and values
        #
        #     node["class"] = entry.__class__.__name__
        #     self.graph[entry.anchor] = node

        game_objects = doc.filter(class_names=["GameObject"])
        for entry in game_objects:
            node = {"class": entry.__class__.__name__}
            self.graph[entry.anchor] = node
        # print(self.graph)

    def build_graph(self):
        G = nx.Graph()
        for anchor in self.graph.keys():
            G.add_node(anchor)
            G.add_edge("user", anchor)
        nx.draw_networkx(G, with_labels=True)
        plt.show()



def parse_unity_file(filename):
    """
    Parses a Unity .unity file (in YAML format) and extracts each object based on header lines.
    Returns a list of dictionaries, each containing 'type', 'id', and 'content' (a list of lines).
    """
    objects = []
    current_object = None
    # Regex to match lines like: --- !u!222 &3409566789090859841
    header_regex = re.compile(r'^--- !u!(\d+)\s+&(\d+)$')
    with open(filename, 'r', encoding='utf-8') as f:
        for line in f:
            stripped_line = line.strip()
            header_match = header_regex.match(stripped_line)
            if header_match:
                # If there's an object being built, add it to the list
                if current_object is not None:
                    objects.append(current_object)
                # Create a new object based on the header
                obj_type = header_match.group(1)
                obj_id = header_match.group(2)
                current_object = {
                    'type': obj_type,
                    'id': obj_id,
                    'content': []
                }
            elif current_object is not None:
                # Append the line to the current object's content
                current_object['content'].append(line.rstrip('\n'))
    # Add the last object if any
    if current_object is not None:
        objects.append(current_object)
    return objects


if __name__ == '__main__':
    # need to set the path of the scene, and also the path of where the prefabs are stored
    scenes = Path("/Users/ruizhengu/Projects/InteractoBot/envs/XRIExample/Assets/Scenes")
    sut = scenes / "SampleScene.unity"

    graph = InteractionGraph()
    graph.unity_parser(sut)
    # graph.build_graph()

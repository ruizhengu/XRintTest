import re
from pathlib import Path
from unityparser import UnityDocument
import networkx as nx
import matplotlib.pyplot as plt


class InteractionGraph:
    def __init__(self, root, sut):
        self.root = root
        self.asset_path = [
            self.root / "Assets",
            self.root / "Library"
        ]
        self.graph = {}
        self.sut = sut
        self.assets = self.get_assets()
        self.unity_doc = UnityDocument.load_yaml(self.sut)

    def get_assets(self):
        results = {}
        for path in self.asset_path:
            for asset in path.rglob("*.meta"):
                file_name = asset.stem  # Get the file name without the suffix
                with open(asset, 'r', encoding='utf-8') as f:
                    content = f.read()
                    guid_match = re.search(r'guid: (\w+)', content)
                    if guid_match:
                        guid = guid_match.group(1)
                        results[file_name] = {"guid": guid}
        return results

    def get_name_by_guid(self, guid):
        for k, v in self.assets.items():
            if v["guid"] == guid:
                return k
        return None

    def get_script_interactable(self):
        scripts = self.unity_doc.filter(class_names=["MonoBehaviour"])
        scripts_interactable = []
        for entry in scripts:
            script_guid = entry.m_Script.get("guid")
            # Check if this guid exists in any of the assets
            for asset_name, asset_data in self.assets.items():
                if asset_data["guid"] == script_guid and "Interactable" in asset_name:
                    scripts_interactable.append(entry)
                    # matching_scripts.append({
                    #     "anchor": entry.anchor,
                    #     "asset_name": asset_name,
                    #     "guid": script_guid
                    # })
        # TODO: return the object
        return scripts_interactable

    def get_object_interactable(self):
        objects_interactable = []
        for scripts_interactable in self.get_script_interactable():
            if hasattr(scripts_interactable, "m_GameObject"):
                objects_interactable.append(scripts_interactable)
        print(objects_interactable)
        return objects_interactable

    def get_prefab_interactable(self):
        prefabs_interactable = []
        for objects_interactable in self.get_object_interactable():
            if hasattr(objects_interactable, "m_PrefabInstance"):
                prefabs_interactable.append(
                    objects_interactable)
        return prefabs_interactable

    '''
    Example:
    Information from .unity file
    ...
    --- !u!1660057539 &9223372036854775807
    SceneRoots:
    ...

    entry.anchor == 9223372036854775807
    entry.__class__.__name__ == SceneRoots
    '''

    def unity_parser(self):
        game_objects = self.unity_doc.filter(class_names=["GameObject"])
        for entry in game_objects:
            node = {"class": entry.__class__.__name__}
            self.graph[entry.anchor] = node
            # for pro, val in vars(entry).items():
            #     print(pro, val) # property and values
        scripts = self.unity_doc.filter(class_names=["MonoBehaviour"])
        for entry in scripts:
            script = entry.m_Script
            print(script)

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
    root = Path("/Users/ruizhengu/Projects/InteractoBot/envs/XRIExample")
    assets = root / "Assets"
    scenes = assets / "Scenes"
    sut = scenes / "SampleScene.unity"

    graph = InteractionGraph(root, sut)
    # for k, v in graph.assets.items():
    #     if ".cs" in k and "Interactable" in k:
    #         print(k, v)
    # print(graph.get_script_interactable())
    # print(graph.get_object_interactable())
    for prefab in graph.get_prefab_interactable():
        print(prefab)

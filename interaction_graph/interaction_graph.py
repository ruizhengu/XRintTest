import re
from pathlib import Path
from unityparser import UnityDocument
import networkx as nx
import matplotlib.pyplot as plt
import time


class InteractionGraph:
    def __init__(self, root, sut):
        self.root = root
        self.asset_path = [
            self.root / "Assets",
            self.root / "Library"
        ]
        self.graph = {}
        self.sut = sut
        self.scene_doc = UnityDocument.load_yaml(self.sut)

    def get_asset_name_by_guid(self, guid):
        for path in self.asset_path:
            for asset in path.rglob("*.meta"):
                found_guid = self.get_guid(asset)
                if found_guid == guid:
                    return asset.stem  # Get the file name without the suffix
        return None

    def get_file_guid(self, file_name):
        with open(file_name, 'r', encoding='utf-8') as f:
            content = f.read()
            guid_match = re.search(r'guid: (\w+)', content)
            if guid_match:
                found_guid = guid_match.group(1)
                return found_guid
        return None

    def get_scene_prefabs(self):
        '''
        Get all the prefab instances in the scene under test
        '''
        results = []
        prefab_instances = self.scene_doc.filter(
            class_names=("PrefabInstance",), attributes=("m_SourcePrefab",))
        for instance in prefab_instances:
            results.append(instance.m_SourcePrefab.get("guid"))
        return results

    def get_interaction_script(self):
        '''
        Get the scripts that have "Interactable" or "Interactor" in the name
        Based on .meta files and record the guid of the script
        '''
        scripts = {}
        for path in self.asset_path:
            for asset in path.rglob("*.meta"):
                file_name = asset.stem  # Get the file name without the suffix
                if (file_name.endswith(".cs") and ("Interactable" in file_name or "Interactor" in file_name)) and "deprecated" not in file_name:
                    guid = self.get_file_guid(asset)
                    if guid:
                        scripts[file_name] = {"guid": guid}
        return scripts

    def get_interactive_prefab(self):
        '''
        Get the prefab instances (as in prefab files instead from the scene under test) 
        that are interactable (Interactable or Interactor)
        '''
        results = {}
        for prefab in self.get_scene_prefabs():
            for path in self.asset_path:
                for asset in path.rglob("*.meta"):
                    if self.get_file_guid(asset) == prefab:
                        results[asset.stem] = {"guid": prefab}
        return results

    # def get_script_interaction(self):
    #     # Get the guids of the assets with "Interactable" or "Interactor" in the name
    #     interaction_guids = {asset_data["guid"] for asset_name, asset_data in self.assets.items()
    #                          if "Interactable" in asset_name or "Interactor" in asset_name}
    #     scripts = self.unity_doc.filter(
    #         class_names=["MonoBehaviour"])  # Get MonoBehaviour scripts
    #     # Find the scripts that have a guid that is in the interaction_guids
    #     return [entry for entry in scripts if entry.m_Script.get("guid") in interaction_guids]

    # def get_object_interaction(self):
    #     objects = self.unity_doc.filter(class_names=["GameObject"])
    #     scripts_interaction = self.get_script_interaction()
    #     interaction_object_ids = {script.m_GameObject.get("fileID") for script in scripts_interaction
    #                               if hasattr(script, "m_GameObject")}
    #     return [go for go in objects if go.anchor in interaction_object_ids]

    # def get_prefab_interaction(self):
    #     prefabs = self.unity_doc.filter(class_names=["PrefabInstance"])
    #     objects_interaction = self.get_object_interaction()
    #     interactable_prefab_ids = {obj.m_PrefabInstance["fileID"] for obj in objects_interaction
    #                                if hasattr(obj, 'm_PrefabInstance')}
    #     prefabs_interaction = []
    #     for prefab in prefabs:
    #         if prefab.anchor in interactable_prefab_ids and hasattr(prefab, "m_Modification"):
    #             modifications = prefab.m_Modification["m_Modifications"]
    #             for mod in modifications:
    #                 if mod.get("propertyPath") == "m_Name":
    #                     print(mod.get("value"))
    #     return prefabs_interaction

    def unity_parser(self):
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
    # print(graph.get_interaction_script(), len(graph.get_interaction_script()))
    print(graph.get_interactive_prefab())
    # graph.get_assets_prefab()
    # for prefab in graph.get_prefab_interaction():
    #     print(prefab)

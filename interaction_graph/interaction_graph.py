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
        self.interaction_scripts = self.get_interaction_scripts()

    def get_asset_name_by_guid(self, guid):
        for path in self.asset_path:
            for asset in path.rglob("*.meta"):
                found_guid = self.get_file_guid(asset)
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

    def get_entry_by_anchor(self, anchor):
        for entry in self.scene_doc.entries:
            if entry.anchor == anchor:
                return entry
        return None

    def get_scene_prefabs(self):
        '''
        Get all the prefab instances in the scene under test
        '''
        prefab_guids = []
        prefab_instances = self.scene_doc.filter(
            class_names=("PrefabInstance",), attributes=("m_SourcePrefab",))
        for instance in prefab_instances:
            prefab_guids.append(instance.m_SourcePrefab.get("guid"))
        return prefab_guids

    def get_interaction_scripts(self):
        '''
        Get the scripts that have "Interactable" or "Interactor" in the name
        Based on .meta files and record the guid of the script
        '''
        scripts = {}
        for path in self.asset_path:
            for asset in path.rglob("*.meta"):
                file_name = asset.stem  # Get the file name without the suffix
                if (file_name.endswith(".cs") and ("Interactable" in file_name or "Interactor" in file_name)) and "deprecated" not in file_name:
                    if guid := self.get_file_guid(asset):
                        scripts[file_name] = {
                            "guid": guid, "file": asset}
        return scripts

    def get_interactive_prefabs(self):
        '''
        Get prefab instances from the scene that are either Interactable or Interactor
        '''
        results = {'interactables': [], 'interactors': []}
        # Convert to set for O(1) lookup
        prefab_guids = set(self.get_scene_prefabs())
        # Find prefab files that match scene prefab GUIDs
        prefab_files = {}
        for path in self.asset_path:
            for meta_file in path.rglob("*.prefab.meta"):
                if meta_file.stem.endswith(".prefab"):
                    guid = self.get_file_guid(meta_file)
                    if guid in prefab_guids:
                        prefab_name = meta_file.stem
                        prefab_files[prefab_name] = meta_file.parent / \
                            prefab_name
        # Check each prefab's scripts for interactable/interactor components
        for prefab_name, prefab_path in prefab_files.items():
            prefab_doc = UnityDocument.load_yaml(prefab_path)
            script_guids = {script.m_Script.get("guid") for script in
                            prefab_doc.filter(class_names=("MonoBehaviour",), attributes=("m_Script",))}
            # Check if prefab has any interaction scripts
            for script_name, script_data in self.interaction_scripts.items():
                if script_data["guid"] in script_guids:
                    if "Interactable" in script_name:
                        results["interactables"].append(prefab_name)
                    elif "Interactor" in script_name:
                        results["interactors"].append(prefab_name)
                    break  # Found an interaction script, no need to check others
        return results

    def get_scene_interactives(self):
        '''
        Get the interactables and interactors in the scene under test
        '''
        results = []
        # Get interaction script guids and matching MonoBehaviours
        interaction_script_guids = {
            data["guid"] for _, data in self.interaction_scripts.items()}
        scene_scripts = self.scene_doc.filter(
            class_names=("MonoBehaviour",), attributes=("m_Script",))
        # Get interactive GO IDs and their prefab IDs
        game_object_ids = [script.m_GameObject.get("fileID") for script in scene_scripts
                           if script.m_Script.get("guid") in interaction_script_guids]
        prefab_ids = []
        for obj_id in game_object_ids:
            if entry := self.get_entry_by_anchor(obj_id):
                if prefab_id := entry.m_PrefabInstance.get("fileID"):
                    prefab_ids.append(prefab_id)
        # Get prefab names
        for prefab_id in prefab_ids:
            if entry := self.get_entry_by_anchor(prefab_id):
                results.append(entry)
                # for mod in entry.m_Modification["m_Modifications"]:
                #     if mod.get("propertyPath") == "m_Name":
                #         print(mod.get("value"))
        return results

    def get_interactors_interactables(self):
        '''
        Categorize scene interactives and interactive prefabs into interactables and interactors
        Returns: Dictionary with two lists - 'interactables' and 'interactors'
        '''
        result = self.get_interactive_prefabs()
        return result

    def get_interactive_ui(self):
        # TODO: Need to get the interactive UI in the scene under test
        pass

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

    def test(self):
        interactive_prefabs = self.get_interactive_prefabs()
        print(interactive_prefabs, len(interactive_prefabs["interactables"]), len(
            interactive_prefabs["interactors"]))
        # scene_interactives = self.get_scene_interactives()
        # print(scene_interactives, len(scene_interactives))
        # categorized = graph.get_interactors_interactables()
        # print("Interactables:", len(categorized['interactables']))
        # print("Interactors:", len(categorized['interactors']))


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
    graph.test()
    # print(graph.get_asset_name_by_guid("77e7c27b2c5525e4aa8cc9f99d654486"))

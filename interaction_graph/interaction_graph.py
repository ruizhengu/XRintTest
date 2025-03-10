import re
from pathlib import Path
from unityparser import UnityDocument
import networkx as nx
import matplotlib.pyplot as plt
import time
import functools


def log_execution_time(func):
    '''
    Decorator to log the execution time of a function
    '''
    @functools.wraps(func)
    def wrapper(*args, **kwargs):
        start_time = time.time()
        result = func(*args, **kwargs)
        end_time = time.time()
        print(f"Execution time: {end_time - start_time} seconds")
        return result
    return wrapper


def cache_result(func):
    '''
    Decorator to cache the result of a function.
    Creates a cache dictionary for each decorated function to store results.
    The cache is stored as an instance attribute on the class.
    Args:
        func: The function to be decorated
    Returns:
        wrapper: The wrapped function that implements caching
    '''
    @functools.wraps(func)
    def wrapper(self, *args, **kwargs):
        # Create unique cache attribute name for this function
        cache_attr = f"_{func.__name__}_cache"
        # Create unique key based on args and kwargs
        cache_key = str(args) + str(kwargs)
        # Initialize cache dict if it doesn't exist
        if not hasattr(self, cache_attr):
            setattr(self, cache_attr, {})
        # Use *getattr* instead of *setattr* to add a new key to the cache
        cache = getattr(self, cache_attr)
        # Return cached result if it exists, otherwise compute and cache
        if cache_key not in cache:
            cache[cache_key] = func(self, *args, **kwargs)
        return cache[cache_key]
    return wrapper


class InteractionGraph:
    def __init__(self, root, sut):
        self.root = root
        self.asset_path = [
            self.root / "Assets",
            self.root / "Library"
        ]
        self.sut = sut
        self.scene_doc = UnityDocument.load_yaml(self.sut)
        self.interaction_scripts = self.get_interaction_scripts()

    @cache_result
    def get_assets(self, suffix=".meta"):
        '''
        Get all assets based on the asset paths, default to .meta files
        '''
        assets = []
        for path in self.asset_path:
            for asset in path.rglob(suffix):
                assets.append(asset)
        return assets

    def get_asset_name_by_guid(self, guid):
        '''
        Get the asset name by the guid
        '''
        for asset in self.get_assets():
            found_guid = self.get_file_guid(asset)
            if found_guid == guid:
                return asset.stem  # Get the file name without the suffix
        return None

    def get_file_guid(self, file_name):
        '''
        Get the guid of the file
        '''
        with open(file_name, 'r', encoding='utf-8') as f:
            content = f.read()
            guid_match = re.search(r'guid: (\w+)', content)
            if guid_match:
                found_guid = guid_match.group(1)
                return found_guid
        return None

    def get_entry_by_anchor(self, anchor):
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
        for entry in self.scene_doc.entries:
            if entry.anchor == anchor:
                return entry
        return None

    def get_prefabs_source_from_scene(self):
        '''
        Get the prefab source according to the prefabs in the scene
        '''
        prefab_guids = set(self.get_scene_prefabs())
        prefab_files = {}
        # Find prefab source files that match scene prefab GUIDs
        for meta_file in self.get_assets("*.prefab.meta"):
            guid = self.get_file_guid(meta_file)
            if guid in prefab_guids:
                prefab_name = meta_file.stem
                prefab_files[prefab_name.replace(".prefab", "")] = meta_file.parent / \
                    prefab_name
        return prefab_files

    @cache_result
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
        for asset in self.get_assets("*.cs.*"):
            file_name = asset.stem  # Get the file name without the suffix
            if (("Interactable" in file_name or "Interactor" in file_name)) and "deprecated" not in file_name:
                if guid := self.get_file_guid(asset):
                    scripts[file_name] = {
                        "guid": guid, "file": asset}
        return scripts

    def get_interactive_prefabs(self):
        '''
        Get prefab instances from the scene that are either Interactable or Interactor
        '''
        results = {'interactables': [], 'interactors': []}
        # get prefab source files within the scene
        prefab_files = self.get_prefabs_source_from_scene()
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
        results = {'interactables': [], 'interactors': []}
        scene_scripts = self.scene_doc.filter(
            class_names=("MonoBehaviour",), attributes=("m_Script",))
        # Map script guids to their type (interactable/interactor)
        script_type_map = {}
        for _, data in self.interaction_scripts.items():
            guid = data["guid"]
            if "Interactable" in str(data["file"]):
                script_type_map[guid] = "interactables"
            elif "Interactor" in str(data["file"]):
                script_type_map[guid] = "interactors"
        # Process each script and collect game object IDs by type
        game_objects = {'interactables': [], 'interactors': []}
        for script in scene_scripts:
            guid = script.m_Script.get("guid")
            if guid in script_type_map:
                obj_type = script_type_map[guid]
                game_objects[obj_type].append(
                    script.m_GameObject.get("fileID"))
        # Get prefab names for both types
        for obj_type in ('interactables', 'interactors'):
            for obj_id in game_objects[obj_type]:
                if entry := self.get_entry_by_anchor(obj_id):
                    if prefab_id := entry.m_PrefabInstance.get("fileID"):
                        if prefab_entry := self.get_entry_by_anchor(prefab_id):
                            if name := self.get_prefab_instance_name(prefab_entry):
                                results[obj_type].append(name)
        return results

    def get_interactive_uis(self):
        '''
        Recursively search for UI objects in prefabs (m_Delegates)
        '''
        def has_delegates_in_prefab(prefab_path, processed):
            '''
            Recursive search method
            '''
            if prefab_path in processed:
                return False
            processed.add(prefab_path)
            try:
                prefab_doc = UnityDocument.load_yaml(prefab_path)
                # Check for delegates in current prefab
                if prefab_doc.filter(class_names=("MonoBehaviour",), attributes=("m_Delegates",)):
                    return True
                # Check nested prefabs
                for instance in prefab_doc.filter(class_names=("PrefabInstance",), attributes=("m_SourcePrefab",)):
                    if source_guid := instance.m_SourcePrefab.get("guid"):
                        for meta_file in self.get_assets("*.prefab.meta"):
                            if self.get_file_guid(meta_file) == source_guid:
                                nested_path = meta_file.parent / meta_file.stem
                                if has_delegates_in_prefab(nested_path, processed):
                                    return True
            except Exception as e:
                print(f"Error processing prefab {prefab_path}: {e}")
            return False
        uis = []
        processed_prefabs = set()
        for name, path in self.get_prefabs_source_from_scene().items():
            if has_delegates_in_prefab(path, processed_prefabs):
                uis.append(name)
        return uis

    def get_scene_uis(self):
        '''
        Get all the UI objects in the scene under test (based on m_Delegates)
        '''
        uis = []
        delegates = self.scene_doc.filter(
            class_names=("MonoBehaviour",), attributes=("m_Delegates",))
        for delegate in delegates:
            object = self.get_entry_by_anchor(
                delegate.m_GameObject.get("fileID"))
            uis.append(object.m_Name)
        return uis

    def get_prefab_instance_name(self, prefab_entry):
        for mod in prefab_entry.m_Modification["m_Modifications"]:
            if mod.get("propertyPath") == "m_Name":
                return mod.get("value")
        return None

    def get_interactors_interactables(self):
        '''
        Categorize scene interactives and interactive prefabs into interactables and interactors
        Returns: Dictionary with two lists - 'interactables' and 'interactors'
        '''
        prefab_results = self.get_interactive_prefabs()
        scene_results = self.get_scene_interactives()
        uis = self.get_interactive_uis()
        uis += self.get_scene_uis()
        merged_results = {
            'interactables': prefab_results['interactables'] + scene_results['interactables'] + uis,
            'interactors': prefab_results['interactors'] + scene_results['interactors']
        }
        print(
            f"Interactors: {merged_results['interactors']}, length: {len(merged_results['interactors'])}")
        print(
            f"Interactables: {prefab_results['interactables'] + scene_results['interactables']}, length: {len(prefab_results['interactables'] + scene_results['interactables'])}")
        print(f"UIs: {uis}, length: {len(uis)}")
        return merged_results

    @log_execution_time
    def build_graph(self):
        interactives = self.get_interactors_interactables()
        G = nx.Graph()
        edge_labels = {}
        for interactor in interactives['interactors']:
            G.add_node(interactor)
        event_count = 1
        for interactable in interactives['interactables']:
            G.add_node(interactable)
            G.add_edge(interactor, interactable)
            edge_labels[(interactor, interactable)] = f"e_{event_count}"
            event_count += 1
        nx.draw_networkx(G, pos=nx.spring_layout(G), with_labels=True)
        nx.draw_networkx_edge_labels(
            G, pos=nx.spring_layout(G), edge_labels=edge_labels)
        # plt.show()

    def test(self):
        # print(self.get_interactive_prefabs())
        graph.build_graph()
        # print(self.get_scene_uis())
        # print(self.get_interactive_uis())


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

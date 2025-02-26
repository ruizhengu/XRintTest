import re
from pathlib import Path


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
    parsed_objects = parse_unity_file(sut)
    for obj in parsed_objects:
        print(f"Object Type: {obj['type']}, ID: {obj['id']}")
        print("Content preview:")
        # Print first few lines of the object's content for preview
        for line in obj['content'][:5]:
            print("  " + line)
        print("-----")

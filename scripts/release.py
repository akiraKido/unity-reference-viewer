#!/usr/bin/env python

import os
from pathlib import Path
from distutils.dir_util import copy_tree
import json
import semver
import argparse
import shutil

script_dir_path = Path(os.path.dirname(os.path.realpath(__file__)))
root_path = Path(script_dir_path.parent)
build_path = Path(root_path, "builds")
project_path = Path(root_path, "src", "UnityReferenceViewer")
package_path = Path(project_path, "Assets", "UnityReferenceViewer")
package_json_path = Path(package_path, "package.json")

def copy_files_to_build():
    if os.path.exists(build_path):
        shutil.rmtree(build_path)
    os.mkdir(build_path)
    copy_tree(str(package_path), str(build_path))

def update_version(bump: str):
    with open(package_json_path, 'r') as file:
        content = json.load(file)

    bump_funtion = {
        "major": semver.bump_major,
        "minor": semver.bump_minor,
        "patch": semver.bump_patch
    }.get(bump)

    if not bump:
        raise ValueError(f"Invalid version_type: '{bump}'")
    
    content["version"] = bump_funtion(content["version"])
    print(f"updating version to {content['version']}")

    with open(package_json_path, 'w') as file:
        json.dump(content, file, indent=2)

if __name__ == '__main__':

    parser = argparse.ArgumentParser(
        prog="Update script",
        description="Updates the version of the package and creates a copy to the builds folder."
    )

    group = parser.add_mutually_exclusive_group(required=True)
    group.add_argument('-mm', '--major', action='store_true', help="bump major version")
    group.add_argument('-m', '--minor', action='store_true', help="bump minor version")
    group.add_argument('-p', '--patch', action='store_true', help="bump patch version")

    args = parser.parse_args()

    print("updating version...")
    if args.major:
        update_version("major")
    elif args.minor:
        update_version("minor")
    elif args.patch:
        update_version("patch")

    print("copying files...")
    copy_files_to_build()

    print("complete")

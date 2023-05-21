#!/usr/bin/env python

import shutil
import os
from pathlib import Path
from distutils.dir_util import copy_tree

script_dir_path = Path(os.path.dirname(os.path.realpath(__file__)))
root_path = Path(script_dir_path.parent)
build_path = Path(root_path, "builds")
project_path = Path(root_path, "src", "UnityReferenceViewer")
package_path = Path(project_path, "Assets", "UnityReferenceViewer")

def copy_files_to_build():
    copy_tree(str(package_path), str(build_path))

copy_files_to_build()

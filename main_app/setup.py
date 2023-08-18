
from setuptools import (
    setup, 
)
import os

project_name = 'main_app'
project_version = '1.0'

packages = {
    'main_app' : '.',
    'api_logging' : './main_app'
}

setup(
    name = project_name, 
    version = project_version, 
    packages = packages.keys(),
    package_dir = packages
)
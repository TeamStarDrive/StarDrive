#!/usr/bin/python3
import os, argparse, hashlib
from typing import List, Dict, Iterable
from DeployUtils import console

BIN_EXTENSIONS = ['.dll', '.pdb', '.config', '.exe']
BIN_EXCLUDE = ['SDNativeTests.exe', 'SDNativeTests.pdb', 'SDNative.pdb']

parser = argparse.ArgumentParser()
parser.add_argument('--root_dir', type=str, help='BlackBox/ repository root directory')
parser.add_argument('--major', action='store_true', help='Is this a major release?')
parser.add_argument('--patch', action='store_true', help='Is this a cumulative patch?')
args = parser.parse_args()

def path_combine(a, b):
    return os.path.normpath(os.path.join(a, b))

def read_lines(listfile) -> List[str]:
    lines: List[str] = []
    if os.path.exists(listfile):
        with open(listfile, 'r') as f:
            for line in f.readlines():
                line = line.strip()
                if len(line) != 0 and not line.startswith('#'):
                    lines.append(line)
    return lines

class FileInfo:
    def __init__(self, game_dir, relpath, hash=None):
        self.filename = relpath
        self.hash = FileInfo.get_hash(game_dir, relpath) if not hash else hash

    def __str__(self): return self.hash + ';' + self.filename
    def __repr__(self): return self.hash + ';' + self.filename

    @staticmethod
    def get_hash(game_dir, filename) -> str:
        hasher = hashlib.sha1()
        with open(path_combine(game_dir, filename), 'rb') as f:
            hasher.update(f.read())
        return hasher.hexdigest()

    @staticmethod
    def load_file_infos(game_dir:str, listfile:str) -> List['FileInfo']:
        files = []
        for line in read_lines(listfile):
            parts = line.split(';')
            files.append(FileInfo(game_dir, relpath=parts[1], hash=parts[0]))
        return files

    @staticmethod
    def load_file_infos_dict(game_dir:str, listfile:str) -> List['FileInfo']:
        if os.path.exists(listfile):
            return FileInfo.dict(FileInfo.load_file_infos(game_dir, listfile))
        return dict()

    @staticmethod
    def dict(files: List['FileInfo']) -> Dict[str, 'FileInfo']:
        return dict([(f.filename, f) for f in files])

    @staticmethod
    def save_file_infos(filename:str, file_infos: List['FileInfo']):
        console(f'Write FileInfos: {filename} ({len(file_infos)} files)')
        text = '\n'.join([str(f) for f in file_infos])
        with open(filename, 'w') as f: f.write(text)

    @staticmethod
    def list_files_recursive(game_dir:str, subdir:str) -> List['FileInfo']:
        files: List[FileInfo] = []
        for (dirpath, _, filenames) in os.walk(path_combine(game_dir, subdir)):
            dirname = dirpath.replace(game_dir, '')
            for f in filenames:
                files.append(FileInfo(game_dir, path_combine(dirname, f), hash=None))
        return files

    @staticmethod
    def list_files(game_dir, subdir, extensions, exclude) -> List['FileInfo']:
        files: List[FileInfo] = []
        for f in os.listdir(path_combine(game_dir, subdir)):
            if os.path.splitext(f)[1].lower() in extensions:
                if not f in exclude:
                    files.append(FileInfo(game_dir, f, hash=None))
        return files

def create_installer_commands(filename:str,
                              new_files:Iterable[FileInfo],
                              deleted_files:Iterable[FileInfo] = [],
                              delete_folders:List[str] = [],
                              major_release=False):
    lines = []
    if major_release: # in major releases, destroy any old Content files to save us from incompatibility issues
        lines.append(f'RMDir /r "$INSTDIR\\Content"\n')
    else:
        for delete in deleted_files:
            lines.append(f'Delete "$INSTDIR\\{delete.filename}"\n')
        for dir_to_delete in delete_folders:
            lines.append(f'RMDir "$INSTDIR\\{dir_to_delete}"\n')

    created_paths = set()
    for new in new_files:
        folder = os.path.dirname(new.filename)
        if folder and not folder in created_paths:
            created_paths.add(folder)
            lines.append(f'CreateDirectory "$INSTDIR\{folder}"\n')
        lines.append(f'File "/oname={new.filename}" "${{SOURCE_DIR}}\game\{new.filename}"\n')

    console(f'Write Installer Commands: {filename} ({len(lines)} commands)')
    with open(filename, 'w') as f:
        f.writelines(lines)

def create_installer_files_list(major=False, patch=False, version='1.0.11000'):
    blackbox_dir = args.root_dir if args.root_dir else os.getcwd()
    game_dir = path_combine(blackbox_dir, 'game') + '\\'

    vmajor,vminor,vpatch = version.split('.')
    version = f'{vmajor}.{vminor}'
    console(f'{"Major" if major else "Patch"} Version: {version}')

    major_release_file = path_combine(blackbox_dir, f'Deploy\\Versions\\Release.{version}.txt')
    delete_files_path = path_combine(blackbox_dir, f'Deploy\\Versions\\Release.{version}.DeleteFiles.txt')
    delete_dirs_path = path_combine(blackbox_dir, f'Deploy\\Versions\\Release.{version}.DeleteDirs.txt')
    new_files_path = path_combine(blackbox_dir, f'Deploy\\Versions\\Release.{version}.NewOrChanged.txt')
    installer_commands = path_combine(blackbox_dir, 'Deploy\\GeneratedFilesList.nsh')

    known_files = []
    known_files += FileInfo.list_files(game_dir, '', BIN_EXTENSIONS, BIN_EXCLUDE)
    known_files += FileInfo.list_files_recursive(game_dir, 'Content')

    if major:
        FileInfo.save_file_infos(major_release_file, known_files)
        create_installer_commands(installer_commands, known_files, major_release=True)
    elif patch:
        major_files_dict = FileInfo.load_file_infos_dict(game_dir, major_release_file)
        known_files_dict = FileInfo.dict(known_files)
        deleted_files = FileInfo.load_file_infos_dict(game_dir, delete_files_path)
        delete_dirs = read_lines(delete_dirs_path)
        new_files: Dict[str, FileInfo] = dict()

        for file in major_files_dict.values():
            if not file.filename in known_files_dict:
                deleted_files[file.filename] = file

        for file in known_files:
            if not file.filename in major_files_dict:
                new_files[file.filename] = file

        for file in known_files:
            if file.filename in deleted_files or file.filename in new_files:
                continue
            old_file = major_files_dict[file.filename]
            if old_file.hash != file.hash:
                new_files[file.filename] = file

        FileInfo.save_file_infos(delete_files_path, deleted_files.values())
        FileInfo.save_file_infos(new_files_path, new_files.values())
        create_installer_commands(installer_commands, new_files.values(), deleted_files.values(), delete_dirs)

if __name__ == "__main__":
    if args.major: create_installer_files_list(major=True)
    elif args.patch: create_installer_files_list(patch=True)
    else: raise Exception('--major or --patch argument required')

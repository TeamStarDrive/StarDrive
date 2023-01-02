#!/usr/bin/python3
import os, argparse, hashlib, uuid, shutil
from typing import List, Dict, Iterable
from DeployUtils import console, env

BIN_EXTENSIONS = ['.dll', '.pdb', '.config', '.exe']
BIN_EXCLUDE = ['SDNativeTests.exe', 'SDNativeTests.pdb', 'SDNative.pdb']

parser = argparse.ArgumentParser()
parser.add_argument('--root_dir', type=str, help='BlackBox/ repository root directory')
parser.add_argument('--major', action='store_true', help='Is this a major release?')
parser.add_argument('--patch', action='store_true', help='Is this a cumulative patch?')
parser.add_argument('--type', type=str, help='Type of installer: nsis, zip, msi', default='nsis')
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

def new_guid():
    return str(uuid.uuid4()).upper()

class FileInfo:
    def __init__(self, game_dir, relpath, guid, hash=None):
        self.filename = relpath
        self.guid = guid
        self.hash = FileInfo.get_hash(game_dir, relpath) if not hash else hash

    def __str__(self): return self.hash + ';' + self.guid + ';' + self.filename
    def __repr__(self): return self.hash + ';' + self.guid + ';' + self.filename

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
            files.append(FileInfo(game_dir, relpath=parts[2], guid=parts[1], hash=parts[0]))
        return files

    @staticmethod
    def load_file_infos_dict(game_dir:str, listfile:str, required:bool) -> List['FileInfo']:
        if os.path.exists(listfile):
            return FileInfo.dict(FileInfo.load_file_infos(game_dir, listfile))
        if required:
            raise FileNotFoundError(listfile)
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
                files.append(FileInfo(game_dir, path_combine(dirname, f), guid=new_guid(), hash=None))
        return files

    @staticmethod
    def list_files(game_dir, subdir, extensions, exclude) -> List['FileInfo']:
        files: List[FileInfo] = []
        for f in os.listdir(path_combine(game_dir, subdir)):
            if os.path.splitext(f)[1].lower() in extensions:
                if not f in exclude:
                    files.append(FileInfo(game_dir, f, guid=new_guid(), hash=None))
        return files


def create_nsis_commands(new_files:Iterable[FileInfo], deleted_files:Iterable[FileInfo], major:bool):
    lines = []
    if major: # in major releases, destroy any old Content files to save us from incompatibility issues
        lines.append(f'RMDir /r "$INSTDIR\\Content"\n')
    else:
        for delete in deleted_files:
            lines.append(f'Delete "$INSTDIR\\{delete.filename}"\n')

    created_paths = set()
    for new in new_files:
        folder = os.path.dirname(new.filename)
        if folder and not folder in created_paths:
            created_paths.add(folder)
            lines.append(f'CreateDirectory "$INSTDIR\{folder}"\n')
        lines.append(f'File "/oname={new.filename}" "${{SOURCE_DIR}}\\game\\{new.filename}"\n')
    return lines


## The max length of an ID is 72 characters, anything over that is going to cause errors
## This remaps and ID-s that go over that limit
MSI_IDS = dict()

def generate_stupid_msi_id(path: str):
    id = path.replace('/', '_') \
               .replace('\\', '_') \
               .replace(' ', '_') \
               .replace('-', '_') \
               .replace('.', '_')

    if len(id) <= 70: return id 
    if id in MSI_IDS: return MSI_IDS[id]

    uniqueNumber = 'id' + str(len(MSI_IDS) + 1)
    toErase = len(uniqueNumber)+(len(id)-70)
    uniqueId = uniqueNumber + id[toErase:]
    MSI_IDS[id] = uniqueId
    return uniqueId


class MsiFileInfo(FileInfo):
    def __init__(self, info:FileInfo):
        self.filename = info.filename
        self.hash = info.hash
        self.guid = info.guid
        self.id = generate_stupid_msi_id(info.filename)


class DirectoryInfo:
    def __init__(self, fullpath, name):
        self.path = fullpath
        self.name = name
        self.id = generate_stupid_msi_id(fullpath) if fullpath else 'INSTALLFOLDER'
        self.subdirs : List["DirectoryInfo"] = []
        self.files : List[MsiFileInfo] = []
    def findOrCreate(self, fullpath, name) -> "DirectoryInfo":
        for subdir in self.subdirs:
            if subdir.name == name: return subdir
        subdir = DirectoryInfo(fullpath, name)
        self.subdirs.append(subdir)
        return subdir
    def findOrCreateRecursive(self, dirpath:str) -> "DirectoryInfo":
        dirInfo = self
        if dirpath:
            fullpath = None
            for part in dirpath.split('/'):
                fullpath = (fullpath + '/' + part) if fullpath else part
                dirInfo = dirInfo.findOrCreate(fullpath, part)
        return dirInfo


def create_stupid_msi_directory_tree(new_files:Iterable[FileInfo]) -> DirectoryInfo:
    root = DirectoryInfo('', '')
    for new in new_files:
        dirpath = os.path.dirname(new.filename).replace('\\', '/').rstrip('/')
        dirInfo = root.findOrCreateRecursive(dirpath)
        dirInfo.files.append(MsiFileInfo(new))
    return root


def append_stupid_msi_directory_structure(lines:list, dir:DirectoryInfo, indent):
    ind = ' ' * indent
    if dir.subdirs:
        lines.append(f'{ind}<Directory Id="{dir.id}" Name="{dir.name}">\n')
        for subdir in dir.subdirs: append_stupid_msi_directory_structure(lines, subdir, indent+2)
        lines.append(f'{ind}</Directory>\n')
    else:
        lines.append(f'{ind}<Directory Id="{dir.id}" Name="{dir.name}" />\n')


def append_stupid_msi_files(lines:list, dir:DirectoryInfo):
    lines.append(f'  <DirectoryRef Id="{dir.id}">\n')
    for file in dir.files:
        lines.append(f'    <Component Id="C_{file.id}" Guid="{file.guid}"> <File Id="{file.id}" Source="$(var.SourceDir){file.filename}" KeyPath="yes" /> </Component>\n')
    lines.append(f'  </DirectoryRef>\n')
    for subdir in dir.subdirs:
        append_stupid_msi_files(lines, subdir)


def append_stupid_msi_file_components(lines:list, dir:DirectoryInfo):
    for file in dir.files:
        lines.append(f'    <ComponentRef Id="C_{file.id}" />\n')
    for subdir in dir.subdirs:
        append_stupid_msi_file_components(lines, subdir)


def create_msi_commands(new_files:Iterable[FileInfo], deleted_files:Iterable[FileInfo], major:bool):
    lines = []
    lines.append('<?xml version="1.0" encoding="utf-8"?>\n')
    lines.append('<Include>\n')
    lines.append('  <?define SourceDir = "$(var.StarDrive.TargetDir)" ?>\n')

    lines.append('  <!-- Folder Structure -->\n')
    lines.append('  <DirectoryRef Id="INSTALLFOLDER">\n')
    rootDir = create_stupid_msi_directory_tree(new_files)
    for subdir in rootDir.subdirs: append_stupid_msi_directory_structure(lines, subdir, 4)
    lines.append('  </DirectoryRef>\n')

    if deleted_files:
        lines.append('  <!-- Delete Files -->\n')
        for delete in deleted_files:
            folderPath = os.path.dirname(delete.filename)
            filename = os.path.basename(delete.filename)

            dir = rootDir.findOrCreateRecursive()
            dir = DirectoryInfo(folderPath, os.path.basename(folderPath))
            lines.append(f'  <DirectoryRef Id={dir.id}><RemoveFile Name={filename} On="install" /></DirectoryRef>\n')

    lines.append('  <!-- Files -->\n')
    append_stupid_msi_files(lines, rootDir)

    lines.append('  <!-- File Components -->\n')
    lines.append('  <ComponentGroup Id="GameContent" Directory="INSTALLFOLDER">\n')
    append_stupid_msi_file_components(lines, rootDir)
    lines.append('  </ComponentGroup>\n')

    lines.append('</Include>\n')
    return lines


def create_installer_commands(filename:str, new_files:Iterable[FileInfo], deleted_files:Iterable[FileInfo] = [],
                              major=False, type='nsis'):
    lines = []
    if  type == 'nsis': lines = create_nsis_commands(new_files, deleted_files, major)
    elif type == 'msi': lines = create_msi_commands(new_files, deleted_files, major)
    elif type == 'zip': lines = [f'{new.filename}\n' for new in new_files]
    else: raise Exception(f'Unsupported installer type={type}')

    console(f'Write Installer Commands: {filename} ({len(lines)} commands)')
    with open(filename, 'w') as f:
        f.writelines(lines)


def create_installer_files_list(major=False, patch=False, type='nsis'):
    blackbox_dir = args.root_dir if args.root_dir else os.getcwd()
    game_dir = path_combine(blackbox_dir, 'game') + '\\'

    console(f'Generate Installer Files List Dist={"Major" if major else "Patch"} Type={type}')
    os.makedirs('Deploy\\Release', exist_ok=True)
    major_release_file = path_combine(blackbox_dir, f'Deploy\\Release\\Release.txt')
    delete_files_path = path_combine(blackbox_dir, f'Deploy\\Release\\Release.DeleteFiles.txt')
    new_files_path = path_combine(blackbox_dir, f'Deploy\\Release\\Release.NewOrChanged.txt')

    installer_commands_ext = 'txt'
    if type == 'nsis': installer_commands_ext = 'nsh'
    elif type == 'msi': installer_commands_ext = 'wxi'
    installer_commands = path_combine(blackbox_dir, f'Deploy\\GeneratedFilesList.{installer_commands_ext}')

    known_files = []
    known_files += FileInfo.list_files(game_dir, '', BIN_EXTENSIONS, BIN_EXCLUDE)
    known_files += FileInfo.list_files_recursive(game_dir, 'Content')
    known_files += FileInfo.list_files_recursive(game_dir, 'Mods/ExampleMod')

    if major:
        FileInfo.save_file_infos(major_release_file, known_files)
        create_installer_commands(installer_commands, known_files, major=True, type=type)
    elif patch:
        major_files_dict = FileInfo.load_file_infos_dict(game_dir, major_release_file, required=True)
        known_files_dict = FileInfo.dict(known_files)
        deleted_files = FileInfo.load_file_infos_dict(game_dir, delete_files_path, required=False)
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

        # copy the Release.DeleteFiles.txt into the output path
        FileInfo.save_file_infos(delete_files_path, deleted_files.values())
        deleted_files_filename = os.path.basename(delete_files_path)
        shutil.copyfile(delete_files_path, os.path.join(game_dir, deleted_files_filename))
        new_files[deleted_files_filename] = FileInfo(game_dir, deleted_files_filename, guid=new_guid())

        # save new_files and generate installer commands
        FileInfo.save_file_infos(new_files_path, new_files.values())
        create_installer_commands(installer_commands, new_files.values(), deleted_files.values(), type=type)

if __name__ == "__main__":
    if args.major: create_installer_files_list(major=True, type=args.type)
    elif args.patch: create_installer_files_list(patch=True, type=args.type)
    else: raise Exception('--major or --patch argument required')

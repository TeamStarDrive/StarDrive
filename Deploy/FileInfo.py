#!/usr/bin/python3
import os, hashlib
from typing import List, Dict
from DeployUtils import console, new_guid, path_combine, read_lines

class FileInfo:
    def __init__(self, game_dir: str, relpath: str, guid: str, hash=None):
        self.filename = relpath
        self.guid = guid
        self.hash = FileInfo.get_hash(game_dir, relpath) if not hash else hash

    def __str__(self): return self.hash + ';' + self.guid + ';' + self.filename
    def __repr__(self): return self.hash + ';' + self.guid + ';' + self.filename

    @staticmethod
    def get_hash(game_dir, filename) -> str:
        fullpath = path_combine(game_dir, filename)
        if not os.path.exists(fullpath):
            return '' # the file no longer exists
        hasher = hashlib.sha1()
        with open(fullpath, 'rb') as f:
            hasher.update(f.read())
        return hasher.hexdigest()

    @staticmethod
    def load_file_infos(game_dir:str, listfile:str) -> List['FileInfo']:
        files = []
        for line in read_lines(listfile):
            parts = line.split(';')
            relpath = parts[len(parts) - 1] # mandatory
            guid = parts[1] if len(parts) >= 3 else '' # optional
            hash = parts[0] if len(parts) >= 3 else '' # optional
            files.append(FileInfo(game_dir, relpath=relpath, guid=guid, hash=hash))
        return files

    @staticmethod
    def load_file_infos_dict(game_dir:str, listfile:str, required:bool) -> List['FileInfo']:
        if os.path.exists(listfile):
            return FileInfo.dict(FileInfo.load_file_infos(game_dir, listfile))
        if required:
            raise FileNotFoundError(listfile)
        return dict()

    # dictionary by filename (relative path)
    @staticmethod
    def dict(files: List['FileInfo']) -> Dict[str, 'FileInfo']:
        return dict([(f.filename, f) for f in files])

    @staticmethod
    def save_file_infos(filename:str, file_infos: List['FileInfo']):
        console(f'Write FileInfos: {filename} ({len(file_infos)} files)')
        text = '\n'.join([str(f) for f in file_infos])
        with open(filename, 'w', encoding='utf-8') as f:
            f.write(text)

    @staticmethod
    def list_files_recursive(game_dir:str, subdir:str) -> List['FileInfo']:
        files: List['FileInfo'] = []
        for (dirpath, _, filenames) in os.walk(path_combine(game_dir, subdir)):
            dirname = dirpath.replace(game_dir, '')
            for f in filenames:
                files.append(FileInfo(game_dir, path_combine(dirname, f), guid=new_guid(), hash=None))
        return files

    @staticmethod
    def list_files(game_dir, subdir, extensions, exclude) -> List['FileInfo']:
        files: List['FileInfo'] = []
        for f in os.listdir(path_combine(game_dir, subdir)):
            if os.path.splitext(f)[1].lower() in extensions:
                if not f in exclude:
                    files.append(FileInfo(game_dir, f, guid=new_guid(), hash=None))
        return files

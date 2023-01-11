import os, argparse
from typing import Dict
from DeployUtils import console, path_combine
from FileInfo import FileInfo
from pathlib import Path


#####
# Utility which lists all files in the original Steam version of StarDrive
# And compares it to the current BlackBox Content
# Generating a list of vanilla files that should be deleted to avoid any mishaps
#
parser = argparse.ArgumentParser()
parser.add_argument('--stardrive_dir', type=str, help='steamapps/StarDrive/ steam game folder')
parser.add_argument('--blackbox_dir', type=str, help='BlackBox/ repository root directory', default='.')
args = parser.parse_args()

def generate_vanilla_files_to_delete(stardrive_dir: str, blackbox_dir: str, outfile: str):
    if not stardrive_dir: raise Exception('--stardrive_dir argument required')

    stardrive_dir = os.path.abspath(stardrive_dir) + '\\'
    blackbox_game = path_combine(os.path.abspath(blackbox_dir), 'game') + '\\'
    console(f'StarDrive Steam Dir: {stardrive_dir}')
    console(f'BlackBox Game Dir: {blackbox_game}')

    vanilla_files: Dict[str, FileInfo] = FileInfo.dict(FileInfo.list_files_recursive(stardrive_dir, 'Content'))
    console(f'Discovered {len(vanilla_files)} Vanilla files')

    blackbox_files: Dict[str, FileInfo] = FileInfo.dict(FileInfo.list_files_recursive(blackbox_game, 'Content'))
    console(f'Discovered {len(blackbox_files)} BlackBox files')

    console(f'Writing vanilla_files_not_in_blackbox: {outfile}')
    vanilla_files_not_in_blackbox = [f for f in vanilla_files.values() if not f.filename in blackbox_files]
    FileInfo.save_file_infos(outfile, vanilla_files_not_in_blackbox)

    # now for debugging, write all NEW files in BlackBox, we do this to make sure nothing crucial is lost
    outfile_inverse = outfile.replace('DeleteFiles', 'NewFiles')
    if outfile_inverse == outfile: outfile_inverse = outfile + '.NewFiles.txt'
    console(f'Writing blackbox_files_not_in_vanilla: {outfile_inverse}')
    blackbox_files_not_in_vanilla = [f for f in blackbox_files.values() if not f.filename in vanilla_files]
    FileInfo.save_file_infos(outfile_inverse, blackbox_files_not_in_vanilla)

if __name__ == "__main__":
    generate_vanilla_files_to_delete(args.stardrive_dir, args.blackbox_dir, 'Deploy\\Release\\Vanilla.DeleteFiles.txt')

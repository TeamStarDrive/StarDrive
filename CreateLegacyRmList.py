#!/usr/bin/python3
import os, argparse, shutil

#########
# Since most Content is not part of the main repository,
# we have to copy BlackBox/Content into BlackBox/StarDrive/Content
#
# In Debug and Release configurations we only overwrite if files are newer
# In Deploy configration we always copy all BlackBox/Content files and overwrite everything
#
parser = argparse.ArgumentParser()
parser.add_argument('--root_dir', type=str, help='BlackBox/ root directory')
parser.add_argument('--configuration', type=str,
                    help='Build Configuration, Debug, Release or Deploy', default="Debug")
args = parser.parse_args()

def console(message):
    print(message, flush=True)

def robocopy(source, destination, force_overwrite = False):
    type = 'overwrite-all' if force_overwrite else 'copy-if-newer'
    console(f"Copy {source} to {destination}  ({type})")
    # RoboCopy "source" "destination" /options...
    # /e=recursive
    # /xo=eclude-older-files(copy-if-newer), the default behaviour is (overwrite-all)
    # /NFL=no-filename-logs /NDL=no-dirname-logs /NJH=no-job-header
    # /nc=no-fileclass-logs /ns=no-filesize-logs /np=no-progress
    # /MT:8=multi-threaded-copy,8-threads
    f = '' if force_overwrite else '/xo'
    cmd = f'robocopy "{source}" "{destination}" /e {f} /NFL /NDL /NJH /nc /ns /NP /MT:8'
    ret = os.system(cmd)
    # Any retval greater than 8 indicates that there was at least one failure during copy op
    if ret >= 8:
        console(f'command failed with exitcode={ret}: {cmd}')
        exit(-1)

def path_combine(a, b):
    return os.path.normpath(os.path.join(a, b))

def get_legacy_files_to_delete(del_list_file):
    files = []
    with open(del_list_file, 'r') as f:
        for line in f.readlines():
            line = line.strip()
            if len(line) != 0 and not line.startswith('#'):
                files.append(line)
    return files

def generate_installer_rm_list(files_to_delete, outfile):
    lines = [';; These files will be deleted:\n']
    for delete in files_to_delete:
        root, ext = os.path.splitext(delete)
        if ext: lines.append(f'Delete "$INSTDIR\\{delete}"\n')  # file
        else: lines.append(f'RMDir "$INSTDIR\\{delete}"\n')  # folder
    with open(outfile, 'w') as f:
        f.writelines(lines)

def delete_files(game_folder, files_to_delete):
    for delete in files_to_delete:
        delete = path_combine(game_folder, delete)
        if os.path.exists(delete):
            root, ext = os.path.splitext(delete)
            if ext:
                console(f'Delete file: {delete}')
                os.remove(delete)
            else:
                console(f'Delete dir: {delete}')
                shutil.rmtree(delete)


blackbox_dir = args.root_dir
del_list_file = path_combine(blackbox_dir, "game/Content/LegacyContent.txt")
rm_script_file = path_combine(blackbox_dir, "Deploy/LegacyRemove.nsh")

if args.delete_legacy:
    # Load the delete listing file
    files_to_delete = get_legacy_files_to_delete(del_list_file)

    # Generate NSIS install script's RMDir commands
    console(f'Generating installer RM script: {rm_script_file}')
    generate_installer_rm_list(files_to_delete, rm_script_file)


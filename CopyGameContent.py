#!/usr/bin/python3
import os, argparse

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
parser.add_argument('--copy_mods', action='store_true', help='copy BlackBox/Mods into StarDrive/Mods')
args = parser.parse_args()

def robocopy(source, destination, force_overwrite = False):
    type = 'overwrite-all' if force_overwrite else 'copy-if-newer'
    print(f"Copy {source} to {destination}  ({type})")

    # RoboCopy "source" "destination" /options...
    # /e=recursive
    # /xo=eclude-older-files(copy-if-newer), the default behaviour is (overwrite-all)
    # /NFL=no-filename-logs /NDL=no-dirname-logs /NJH=no-job-header
    # /nc=no-fileclass-logs /ns=no-filesize-logs /np=no-progress
    # /MT:8=multi-threaded-copy,8-threads
    f = '' if force_overwrite else '/xo'
    cmd = f'robocopy "{source}" "{destination}" /e {f} /NFL /NDL /NJH /nc /ns /NP /MT:8'
    ret = os.system(cmd)

    # robocopy return vals:
    # 0 - no files were copied, no failure was met
    # 1 - all files copied successfully
    # 2 - Extra files in dst dir? No files were copied
    # 3 - Some file were copied, Additional files present. No failure was met
    # 5 - Some files were mismatched. No failure was met
    # 6 - Additional files and mismatched files exist. No files were copied, no failures met.
    # 7 - Files were copied, a file mismatch was present, and additional files were present
    # 8 - Several files didn't copy
    # Any value greater than 8 indicates that there was at least one failure during copy op
    if ret >= 8:
        print(f'command failed with exitcode={ret}: {cmd}')
        exit(-1)

def path_combine(a, b):
    return os.path.normpath(os.path.join(a, b))

blackbox_dir = args.root_dir
content_src = path_combine(blackbox_dir, "Content")
content_dst = path_combine(blackbox_dir, "StarDrive/Content")

if args.configuration == "Deploy":
    robocopy(content_src, content_dst, force_overwrite=True)
else:
    robocopy(content_src, content_dst, force_overwrite=False)

if args.copy_mods:
    mod_src = path_combine(blackbox_dir, "Mods")
    mod_dst = path_combine(blackbox_dir, "StarDrive/Mods")
    if os.path.exists(mod_src):
        robocopy(mod_src, mod_dst, force_overwrite=False)

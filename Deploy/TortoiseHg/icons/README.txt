Some of these icons originated from the TortoiseSVN project.  We have
modified many of them and added new icons of our own.   All of them are
licensed under the GPLv2.

This software may be used and distributed according to the terms of the
GNU General Public License version 2, incorporated herein by reference.

Some of the icons used here are from the Tango Icon Theme. Some of them
have been modified.

reviewboard.png originated from the Review Board project, which is under
the MIT license.

Directory Structure
-------------------

Icon files should be placed according to xdg-theme-like structure::

    scalable/actions/*.svg ... icons for any size
    24x24/actions/*.png ...... fine-tuned bitmap icons (24x24)
    *.png .................... miscellaneous pixmaps
    *.ico .................... icons used by explorer/nautilus extensions
                               and Windows exe
    svg/*.svg ................ source of .ico files

See also:
http://standards.freedesktop.org/icon-theme-spec/icon-theme-spec-latest.html

Icon Naming
-----------

- Commonly-used icon should have the same name as xdg icons, so that it
  can be replaced by the system theme.

  e.g. `actions/document-new.svg`, `status/folder-open.svg`

- Icon for Mercurial/TortoiseHg-specific operation should be prefixed by
  `hg-` or `thg-`, in order to avoid conflict with the system theme.

  e.g. `actions/hg-incoming.svg`, `actions/thg-sync.svg`

See also:
http://standards.freedesktop.org/icon-naming-spec/icon-naming-spec-latest.html

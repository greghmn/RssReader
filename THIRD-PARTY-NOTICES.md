# Third-Party Notices

RSS Reader incorporates third-party open-source software. The copyrights and licenses of these components belong to their respective rights holders.

RSS Reader's original source code is licensed under the GNU Affero General Public License, version 3 or later (AGPL-3.0-or-later). That license does not replace the licenses governing third-party components.

The full license texts and additional notices are maintained in the [`licenses/`](licenses/) directory.

## Third-Party Components

| Component | Version | License | License and notice files |
|---|---|---|---|
| Avalonia UI | 12.1.2 | MIT | `licenses/MIT-Avalonia.txt` |
| Avalonia ANGLE Windows natives | 2.1.27548.20260419 | BSD-3-Clause | `licenses/BSD-ANGLE.txt` |
| Avalonia.Fonts.Inter / Inter font | 12.1.2 | MIT (Avalonia integration); SIL OFL 1.1 (font) | `licenses/MIT-Avalonia.txt`, `licenses/OFL-Inter.txt` |
| CommunityToolkit.Mvvm | 8.4.2 | MIT | `licenses/MIT-CommunityToolkit.txt` |
| HtmlAgilityPack | 1.13.1 | MIT | `licenses/MIT-HtmlAgilityPack.txt` |
| Microsoft.Data.Sqlite / Microsoft.Data.Sqlite.Core | 10.0.13 | MIT | `licenses/MIT-Microsoft.Data.Sqlite.txt` |
| SQLitePCLRaw | 2.1.13 | Apache-2.0 | `licenses/Apache-2.0-SQLitePCLRaw.txt`, `licenses/NOTICE-SQLitePCLRaw.txt` |
| SQLite database engine | Bundled through SQLitePCLRaw 2.1.13 | Public domain | [SQLite copyright information](https://sqlite.org/copyright.html) |
| SkiaSharp / SkiaSharp.NativeAssets.Win32 | 3.119.4 | MIT package license; additional upstream notices | `licenses/MIT-SkiaSharp.txt`, `licenses/NOTICE-SkiaSharp.txt` |
| HarfBuzzSharp / HarfBuzzSharp.NativeAssets.Win32 | 8.3.1.3 | MIT package license; additional upstream notices | `licenses/MIT-HarfBuzzSharp.txt`, `licenses/NOTICE-HarfBuzzSharp.txt` |
| MicroCom.Runtime | 0.11.6 | MIT | `licenses/MIT-MicroCom.txt` |
| Tmds.DBus.Protocol | 0.94.1 | MIT | `licenses/MIT-Tmds.DBus.txt` |

## Additional Information

Third-party licenses, copyright notices, and disclaimers remain in effect independently of RSS Reader's license.

The framework-dependent Windows release does not bundle the .NET runtime. Users must install an appropriate compatible .NET runtime separately.

The `licenses/` directory also contains notices for other dependencies and distribution formats considered during development. Their presence does not necessarily indicate that those components are included in every release.

## Release Audit Status

The third-party dependency versions listed above have been compared against the dependency inventory generated for the Windows x64 framework-dependent release.

The accompanying `licenses/` directory contains license texts and notices associated with the identified components.

This inventory reflects the dependencies identified during release preparation. It does not constitute an exhaustive audit of every upstream component incorporated into third-party binaries.

Additional license files may be included for dependencies or distribution formats not used in this particular release.

# Third-Party Notices

GreenHellHeadTracking bundles, statically links, or credits the third-party components
listed below. Each remains the property of its authors and is used under its own
licence. Where a licence requires the copyright notice, the conditions and the
disclaimer to accompany a binary distribution, the full text is reproduced here
verbatim, and this file ships at the root of every release ZIP we publish.

This repository contains no Green Hell code, no extracted game assets and no
game data files, and neither release ZIP carries any part of the game. The one
piece of Creepy Jar's work kept here is the README demonstration clip, which is
described under "Green Hell footage" below.

| Component | Version | Licence | How it ships |
|-----------|---------|---------|--------------|
| MelonLoader | v0.6.6 | Apache-2.0 | Bundled verbatim in the installer ZIP |
| cameraunlock-core | 3465659888b2270addac9de0b2a728f59a00360c | MIT | Compiled into `GreenHellHeadTracking.dll` |
| HarmonyX | shipped inside MelonLoader | MIT | Not bundled separately; loaded from MelonLoader at runtime |
| OpenTrack | n/a | ISC | Not bundled; UDP protocol interoperability only |
| Unity API stubs | n/a | our own, MIT | Build-time reference assemblies only; in neither release ZIP |
| Green Hell README clip | n/a | Creepy Jar's, no licence granted | Repository only; in neither release ZIP |

---

## MelonLoader

Vendored at `vendor/melonloader/`, shipped in the installer ZIP and used as the
install-time source. Taken from the upstream release asset untouched; the
upstream licence file ships beside it at `vendor/melonloader/LICENSE`.

- Upstream: https://github.com/LavaGang/MelonLoader
- Version: `v0.6.6`
- Commit: `1119a286590ca78bb4a1217bbd23279e0daac9d3`
- SHA-256: `687b82605606e941cefdc007b880b720922cc319bb70270064590d4038c3c0db`

```
                                 Apache License
                           Version 2.0, January 2004
                        http://www.apache.org/licenses/

   TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION

   1. Definitions.

      "License" shall mean the terms and conditions for use, reproduction,
      and distribution as defined by Sections 1 through 9 of this document.

      "Licensor" shall mean the copyright owner or entity authorized by
      the copyright owner that is granting the License.

      "Legal Entity" shall mean the union of the acting entity and all
      other entities that control, are controlled by, or are under common
      control with that entity. For the purposes of this definition,
      "control" means (i) the power, direct or indirect, to cause the
      direction or management of such entity, whether by contract or
      otherwise, or (ii) ownership of fifty percent (50%) or more of the
      outstanding shares, or (iii) beneficial ownership of such entity.

      "You" (or "Your") shall mean an individual or Legal Entity
      exercising permissions granted by this License.

      "Source" form shall mean the preferred form for making modifications,
      including but not limited to software source code, documentation
      source, and configuration files.

      "Object" form shall mean any form resulting from mechanical
      transformation or translation of a Source form, including but
      not limited to compiled object code, generated documentation,
      and conversions to other media types.

      "Work" shall mean the work of authorship, whether in Source or
      Object form, made available under the License, as indicated by a
      copyright notice that is included in or attached to the work
      (an example is provided in the Appendix below).

      "Derivative Works" shall mean any work, whether in Source or Object
      form, that is based on (or derived from) the Work and for which the
      editorial revisions, annotations, elaborations, or other modifications
      represent, as a whole, an original work of authorship. For the purposes
      of this License, Derivative Works shall not include works that remain
      separable from, or merely link (or bind by name) to the interfaces of,
      the Work and Derivative Works thereof.

      "Contribution" shall mean any work of authorship, including
      the original version of the Work and any modifications or additions
      to that Work or Derivative Works thereof, that is intentionally
      submitted to Licensor for inclusion in the Work by the copyright owner
      or by an individual or Legal Entity authorized to submit on behalf of
      the copyright owner. For the purposes of this definition, "submitted"
      means any form of electronic, verbal, or written communication sent
      to the Licensor or its representatives, including but not limited to
      communication on electronic mailing lists, source code control systems,
      and issue tracking systems that are managed by, or on behalf of, the
      Licensor for the purpose of discussing and improving the Work, but
      excluding communication that is conspicuously marked or otherwise
      designated in writing by the copyright owner as "Not a Contribution."

      "Contributor" shall mean Licensor and any individual or Legal Entity
      on behalf of whom a Contribution has been received by Licensor and
      subsequently incorporated within the Work.

   2. Grant of Copyright License. Subject to the terms and conditions of
      this License, each Contributor hereby grants to You a perpetual,
      worldwide, non-exclusive, no-charge, royalty-free, irrevocable
      copyright license to reproduce, prepare Derivative Works of,
      publicly display, publicly perform, sublicense, and distribute the
      Work and such Derivative Works in Source or Object form.

   3. Grant of Patent License. Subject to the terms and conditions of
      this License, each Contributor hereby grants to You a perpetual,
      worldwide, non-exclusive, no-charge, royalty-free, irrevocable
      (except as stated in this section) patent license to make, have made,
      use, offer to sell, sell, import, and otherwise transfer the Work,
      where such license applies only to those patent claims licensable
      by such Contributor that are necessarily infringed by their
      Contribution(s) alone or by combination of their Contribution(s)
      with the Work to which such Contribution(s) was submitted. If You
      institute patent litigation against any entity (including a
      cross-claim or counterclaim in a lawsuit) alleging that the Work
      or a Contribution incorporated within the Work constitutes direct
      or contributory patent infringement, then any patent licenses
      granted to You under this License for that Work shall terminate
      as of the date such litigation is filed.

   4. Redistribution. You may reproduce and distribute copies of the
      Work or Derivative Works thereof in any medium, with or without
      modifications, and in Source or Object form, provided that You
      meet the following conditions:

      (a) You must give any other recipients of the Work or
          Derivative Works a copy of this License; and

      (b) You must cause any modified files to carry prominent notices
          stating that You changed the files; and

      (c) You must retain, in the Source form of any Derivative Works
          that You distribute, all copyright, patent, trademark, and
          attribution notices from the Source form of the Work,
          excluding those notices that do not pertain to any part of
          the Derivative Works; and

      (d) If the Work includes a "NOTICE" text file as part of its
          distribution, then any Derivative Works that You distribute must
          include a readable copy of the attribution notices contained
          within such NOTICE file, excluding those notices that do not
          pertain to any part of the Derivative Works, in at least one
          of the following places: within a NOTICE text file distributed
          as part of the Derivative Works; within the Source form or
          documentation, if provided along with the Derivative Works; or,
          within a display generated by the Derivative Works, if and
          wherever such third-party notices normally appear. The contents
          of the NOTICE file are for informational purposes only and
          do not modify the License. You may add Your own attribution
          notices within Derivative Works that You distribute, alongside
          or as an addendum to the NOTICE text from the Work, provided
          that such additional attribution notices cannot be construed
          as modifying the License.

      You may add Your own copyright statement to Your modifications and
      may provide additional or different license terms and conditions
      for use, reproduction, or distribution of Your modifications, or
      for any such Derivative Works as a whole, provided Your use,
      reproduction, and distribution of the Work otherwise complies with
      the conditions stated in this License.

   5. Submission of Contributions. Unless You explicitly state otherwise,
      any Contribution intentionally submitted for inclusion in the Work
      by You to the Licensor shall be under the terms and conditions of
      this License, without any additional terms or conditions.
      Notwithstanding the above, nothing herein shall supersede or modify
      the terms of any separate license agreement you may have executed
      with Licensor regarding such Contributions.

   6. Trademarks. This License does not grant permission to use the trade
      names, trademarks, service marks, or product names of the Licensor,
      except as required for reasonable and customary use in describing the
      origin of the Work and reproducing the content of the NOTICE file.

   7. Disclaimer of Warranty. Unless required by applicable law or
      agreed to in writing, Licensor provides the Work (and each
      Contributor provides its Contributions) on an "AS IS" BASIS,
      WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
      implied, including, without limitation, any warranties or conditions
      of TITLE, NON-INFRINGEMENT, MERCHANTABILITY, or FITNESS FOR A
      PARTICULAR PURPOSE. You are solely responsible for determining the
      appropriateness of using or redistributing the Work and assume any
      risks associated with Your exercise of permissions under this License.

   8. Limitation of Liability. In no event and under no legal theory,
      whether in tort (including negligence), contract, or otherwise,
      unless required by applicable law (such as deliberate and grossly
      negligent acts) or agreed to in writing, shall any Contributor be
      liable to You for damages, including any direct, indirect, special,
      incidental, or consequential damages of any character arising as a
      result of this License or out of the use or inability to use the
      Work (including but not limited to damages for loss of goodwill,
      work stoppage, computer failure or malfunction, or any and all
      other commercial damages or losses), even if such Contributor
      has been advised of the possibility of such damages.

   9. Accepting Warranty or Additional Liability. While redistributing
      the Work or Derivative Works thereof, You may choose to offer,
      and charge a fee for, acceptance of support, warranty, indemnity,
      or other liability obligations and/or rights consistent with this
      License. However, in accepting such obligations, You may act only
      on Your own behalf and on Your sole responsibility, not on behalf
      of any other Contributor, and only if You agree to indemnify,
      defend, and hold each Contributor harmless for any liability
      incurred by, or claims asserted against, such Contributor by reason
      of your accepting any such warranty or additional liability.

   END OF TERMS AND CONDITIONS

   Copyright (c) 2020 - 2025 Lava Gang

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
```

### Attribution notice

Apache-2.0 section 4(d) requires the attribution notices from the work's own
NOTICE file to travel with any redistribution. MelonLoader ships its NOTICE at
`MelonLoader/Documentation/NOTICE.txt` inside the archive we redistribute
unmodified, so it reaches the user's game folder intact. Its contents:

```
   MelonLoader
   Copyright 2020 - 2022 Lava Gang

   This product contains software (https://github.com/LavaGang/MelonLoader) developed
   by Lava Gang, licensed under the Apache-2.0 license.
```

The archive also carries the third-party runtime components Lava Gang assembles
into their release (the Mono runtime and class libraries, Mono.Cecil, HarmonyX
and the Il2Cpp tooling), each under its own licence and each accompanied by the
notices Lava Gang ships. We redistribute that archive byte for byte and modify
nothing inside it; the SHA-256 above is the check that this holds.

---

## cameraunlock-core

Git submodule at `cameraunlock-core/`, compiled into `GreenHellHeadTracking.dll`. Our own code,
MIT licensed, reproduced here so the notices are complete.

- Pinned commit: `3465659888b2270addac9de0b2a728f59a00360c`

```
MIT License

Copyright (c) 2026 CameraUnlock

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

---

## OpenTrack

Not bundled and not linked. This mod implements the OpenTrack UDP pose datagram
layout so that OpenTrack (https://github.com/opentrack/opentrack, ISC licence)
and compatible trackers can drive it. No OpenTrack code, headers or binaries
are copied, linked or redistributed, so its licence triggers no notice
obligation here. It is credited because the wire format is its work.

---

## HarmonyX

- **License**: MIT
- **Copyright**: BepInEx contributors (HarmonyX fork)
- **Upstream**: https://github.com/BepInEx/HarmonyX
- **Usage**: Runtime method patching library, shipped inside MelonLoader (not separately bundled by this mod).

---

## Unity API stubs

`src/GreenHellHeadTracking/libs/UnityStubs.cs` and `UnityUIStubs.cs` declare the
Unity and game type signatures this mod compiles against, so that a contributor
or a CI runner can build it without owning Green Hell or installing Unity. They
are written by hand from the public Unity scripting documentation: declarations
and empty bodies only, for interoperability. No Unity source, no decompiled
output and no Unity binary is copied into or distributed by this repository, and
the stub assemblies they compile to are build-time reference assemblies that
appear in neither release ZIP. At runtime the real engine assemblies supplied by
the user's own installed copy of the game are what load.

Unity is a trademark of Unity Technologies, used here only to name the engine
these declarations target.

---

## Green Hell footage

- **File:** `assets/readme-clip.gif`, about twelve seconds of ordinary gameplay
  at 853x480, silent by format.
- **Rights holder:** Creepy Jar S.A., as developer and publisher of Green Hell,
  together with the rights holders of any third-party marks visible in frame.
  The MIT licence in `LICENSE` does not extend to it, and nothing in this
  repository grants or implies any licence to reuse it.
- **What it is:** a screen capture of the game running with this mod, recorded
  by us on a legitimately purchased copy. It is not a trailer, a cutscene, or
  any other marketing or story material. It is
  shown at the top of the README so that someone deciding whether to install
  the mod can see what it actually does, which is how mod pages have presented
  themselves for as long as there have been mod pages.
- **Where it ships:** in this repository only. The packaging scripts copy
  `README.md`, `LICENSE`, `CHANGELOG.md` and this file, and never `assets/`, so
  the clip is in neither the installer ZIP nor the Nexus ZIP, and the launcher
  never deploys it. The README's image URL is absolute, so the copy of the
  README inside a release ZIP still resolves it from GitHub rather than
  shipping it.
- **Takedown:** if Creepy Jar would rather it were not published, open an issue
  on this repository or reach us on the Discord linked in the README and it
  comes down, no argument.

---

## Green Hell

Green Hell is the property of Creepy Jar. This mod is a fan project and is not
affiliated with or endorsed by Creepy Jar. Purchase Green Hell at
https://store.steampowered.com/app/815370/Green_Hell/. Green Hell and all
related names, logos, characters and marks are trademarks of their respective
owners. They are used here only to identify the game this mod applies to,
which is nominative use and not a claim of any right in them. It redistributes
no game code, no extracted game assets and no proprietary DLLs, and it requires
a legitimately purchased copy of the game. The one piece of Creepy Jar's work
kept in this repository is the README demonstration clip described above, which
ships in neither release ZIP. Any engine structure offsets,
function addresses or byte patterns referenced in the source were derived by
the authors through independent analysis of a legitimately owned copy. They
are factual measurements recorded as numbers; no decompiled or disassembled
game code is stored in this repository.

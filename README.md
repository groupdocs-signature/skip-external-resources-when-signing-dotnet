# Skipping External Resources When Signing

[![Product Page](https://img.shields.io/badge/Product%20Page-2865E0?style=for-the-badge&logo=appveyor&logoColor=white)](https://github.com/groupdocs-signature/GroupDocs.Signature-Docs)
[![Docs](https://img.shields.io/badge/Docs-2865E0?style=for-the-badge&logo=Hugo&logoColor=white)](https://docs.groupdocs.com/signature/net/)
[![Blog](https://img.shields.io/badge/Blog-2865E0?style=for-the-badge&logo=WordPress&logoColor=white)](https://blog.groupdocs.com/categories/groupdocs.signature-product-family/)
[![Free Support](https://img.shields.io/badge/Free%20Support-2865E0?style=for-the-badge&logo=Discourse&logoColor=white)](https://forum.groupdocs.com/c/signature/13)
[![Temporary License](https://img.shields.io/badge/Temporary%20License-2865E0?style=for-the-badge&logo=rocket&logoColor=white)](https://purchase.groupdocs.com/temp-license/100124)

## 🚀 Quick Start

`skip-external-resources-when-signing-dotnet` is a runnable .NET 8 console sample built around one behaviour change: from GroupDocs.Signature 26.9, `LoadOptions.SkipExternalResources` defaults to `true`, so a document's linked pictures are no longer fetched while it is loaded. The sample previews the same DOCX three ways - default, whitelisted host, everything allowed - and signs it without reaching the network at all.

`dotnet run`, and the three preview sizes plus the signature count tell the whole story: the default preview is smaller because the linked picture was never downloaded.

## ✨ What You'll Learn

Which document features count as external resources and why fetching them is a server-side request forgery risk, how to allow a specific trusted host without opening the door generally, how to restore the old behaviour for documents you produced yourself, and why signing an untrusted file needs no network access.

### What counts as an external resource?

Linked pictures rather than embedded ones, `INCLUDEPICTURE` fields, linked pictures in presentations and spreadsheets, and the images and style sheets referenced by SVG files. Embedded content is unaffected - it is already inside the file. The distinction matters because only the linked kind causes your server to make an outbound request on behalf of whoever supplied the document.

## 📖 About This Repository

This repository demonstrates safe document loading with GroupDocs.Signature for .NET 26.9. It uses a DOCX whose picture is linked to an image on GitHub rather than embedded, so the difference between loading and skipping is visible in the output file size. The audience is anyone whose service accepts documents from users, e-mail or partner systems and renders or signs them server-side.

The change is a default, not a new feature: the property existed before, and what 26.9 altered is which way it points when you say nothing.

## 🔑 Key Features

### GroupDocs.Signature Capabilities

| Feature | Description |
|---|---|
| **SkipExternalResources** | `true` by default from 26.9; no linked resource is requested during load |
| **WhitelistedResources** | a list of address fragments that may still be fetched, matched case-insensitively |
| **PreviewOptions** | page previews as PNG, with the skipped picture showing as a placeholder |
| **QrCodeSignOptions** | signing works normally without any network access |

### What This Repository Demonstrates

Three previews of the same document under different load settings, with their byte sizes printed, and one signing run that adds a QR code to an untrusted file while fetching nothing. The signed output keeps its link, so an application that opens it later can still resolve the picture itself.

## ⚙️ Prerequisites

.NET SDK 8.0 and GroupDocs.Signature 26.9.0, pinned in `SkipExternalResourcesDemo.csproj`. The whitelist example needs outbound access to `raw.githubusercontent.com`; without it that preview comes back the same size as the default one, and the sample says so rather than failing.

## 📁 Repository Structure

```
skip-external-resources-when-signing-dotnet/
│
├── Program.cs
├── SkipExternalResourcesDemo.csproj
├── documents/
│   └── linked-picture.docx
└── Result/
    ├── preview-default.png
    ├── preview-whitelisted.png
    ├── preview-all.png
    └── signed.docx
```

### File Overview

- **Program.cs** - the four methods below plus a small preview helper
- **documents/linked-picture.docx** - a Word file whose picture is linked, not embedded
- **Result/preview-*.png** - one preview per load setting, for size comparison
- **Result/signed.docx** - the signed output, with its link intact

## 💻 Implementation Examples

### Example 1: Generates a page preview of a document with the default load settings

The safe default. No `LoadOptions` at all, and nothing is fetched.

```csharp
using var signature = new Signature(sourcePath);
return SavePagePreview(signature, previewPath);
```

The preview shows an empty placeholder where the linked picture would be, and the PNG is correspondingly smaller. That is the whole visible effect - and the invisible one is that a crafted document could not make your server call an internal address, leak Windows credentials through a UNC path, or hang waiting for an unreachable host.

### Example 2: Generates a page preview that loads external resources from trusted addresses only

When your own documents legitimately link to a company CDN or an internal image server, allow that host and nothing else.

```csharp
var loadOptions = new LoadOptions
{
    WhitelistedResources = new List<string> { trustedAddress }
};

using var signature = new Signature(sourcePath, loadOptions);
return SavePagePreview(signature, previewPath);
```

Matching is a case-insensitive substring test, so use long fragments: a scheme, host and path. I shortened one of these to a bare host name while testing and it matched a URL I had not intended at all, which is the whole argument for being verbose here. A short fragment like `github` would also match `github.attacker.example`, which is precisely the case the whitelist is supposed to exclude.

### Example 3: Generates a page preview that loads every external resource a document links to

The escape hatch, and the pre-26.9 behaviour.

```csharp
var loadOptions = new LoadOptions { SkipExternalResources = false };

using var signature = new Signature(sourcePath, loadOptions);
return SavePagePreview(signature, previewPath);
```

Use it only for documents you trust - files your own application produced, for instance. Note the obsolete `LoadExternalResources` property means the opposite: `SkipExternalResources = false` replaces `LoadExternalResources = true`, and mixing them up inverts your security posture silently.

### Example 4: Signs an untrusted Word document without fetching anything it links to

The practical case this change was made for: a file arrives from outside, and you need to put a signature on it.

```csharp
using var signature = new Signature(sourcePath);

var options = new QrCodeSignOptions("Approved by GroupDocs.Signature")
{
    EncodeType = QrCodeTypes.QR,
    Left = 400,
    Top = 50,
    Width = 120,
    Height = 120
};

SignResult result = signature.Sign(outputPath, options);
return result.Succeeded.Count;
```

No external resource is requested while the document is loaded, signed or saved. The signed file keeps its link, so a user who opens it in Word later still sees the picture - the link is preserved, it is simply not followed on your server.

### How the preview is written

The previews come from one small helper, included here because `PreviewOptions` takes stream factories rather than a path:

```csharp
var previewOptions = new PreviewOptions(
    pageData => File.Create(previewPath),
    (pageData, pageStream) => pageStream.Dispose())
{
    PreviewFormat = PreviewOptions.PreviewFormats.PNG
};

signature.GeneratePreview(previewOptions);
```

The sample document has a single page, so one file is written per run. For multi-page input you would include the page number in the name.

## 📚 Related Resources

Explore these additional resources for safe document handling with GroupDocs.Signature:

* **Step-by-step use case guide in the documentation** - the three load modes, the whitelist rules, and what to do on upgrade: [Read the article →](https://docs.groupdocs.com/signature/net/use-cases/skip-external-resources/)

* **In-depth blog article about this project** - the SSRF reasoning behind flipping the default, with the three previews side by side: [Read the article →](https://blog.groupdocs.com/signature/skip-external-resources-net/)

* **Generate Document Pages Preview** - the `PreviewOptions` reference used above: [Read the article →](https://docs.groupdocs.com/signature/net/generate-document-pages-preview/)

* **eSign Document with QR Code Signature** - the signing options used in the last example: [Read the article →](https://docs.groupdocs.com/signature/net/esign-document-with-qr-code-signature/)

## 🏷️ Keywords

`external resources`, `ssrf`, `skipexternalresources`, `whitelistedresources`, `loadoptions`, `document security`, `untrusted documents`, `linked picture`, `includepicture`, `document preview`, `groupdocs signature`, `dotnet signing`, `qr code signature`, `server-side request forgery`, `unc path`, `svg`, `net8`, `26.9`, `safe loading`, `docx`, `previewoptions`, `whitelist`

---

**Need help?** [Get Free Support](https://forum.groupdocs.com/c/signature/13) | [Get Temporary License](https://purchase.groupdocs.com/temp-license/100124)

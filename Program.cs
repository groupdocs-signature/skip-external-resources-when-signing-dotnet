// Topic: Load documents safely - external resources are skipped by default since 26.9.
// Uses GroupDocs.Signature for .NET: LoadOptions.SkipExternalResources and
// LoadOptions.WhitelistedResources decide which linked pictures may be fetched.

using GroupDocs.Signature;
using GroupDocs.Signature.Domain;
using GroupDocs.Signature.Options;

namespace Demo.SkipExternalResources;

internal static class Program
{
    private const string DocsFolder = "documents";
    private const string ResultFolder = "Result";

    // The picture in this document is linked to an image on GitHub, not embedded.
    private static readonly string SourceDocx =
        Path.Combine(DocsFolder, "linked-picture.docx");

    // Only addresses that contain this text are loaded in the whitelist example.
    private const string TrustedAddress =
        "https://raw.githubusercontent.com/groupdocs-signature/";

    private static int Main()
    {
        Directory.CreateDirectory(DocsFolder);
        Directory.CreateDirectory(ResultFolder);
        ApplyLicense();

        if (!File.Exists(SourceDocx))
        {
            Console.Error.WriteLine(
                $"Missing source document: {Path.GetFullPath(SourceDocx)}");
            return 1;
        }

        string defaultPreview = Path.Combine(ResultFolder, "preview-default.png");
        string trustedPreview = Path.Combine(ResultFolder, "preview-whitelisted.png");
        string allPreview = Path.Combine(ResultFolder, "preview-all.png");
        string signedDocx = Path.Combine(ResultFolder, "signed.docx");

        long defaultSize = PreviewWithDefaultSettings(SourceDocx, defaultPreview);
        Console.WriteLine($"Default settings   : {defaultSize} bytes, picture skipped");

        long trustedSize =
            PreviewWithWhitelistedHost(SourceDocx, trustedPreview, TrustedAddress);
        Console.WriteLine($"Whitelisted address: {trustedSize} bytes");

        long allSize = PreviewWithAllExternalResources(SourceDocx, allPreview);
        Console.WriteLine($"All resources      : {allSize} bytes");

        if (trustedSize == defaultSize)
        {
            Console.WriteLine("The linked picture could not be downloaded.");
            Console.WriteLine("Check the internet access to raw.githubusercontent.com.");
        }

        int added = SignUntrustedDocument(SourceDocx, signedDocx);
        Console.WriteLine($"Signatures added without loading external resources: {added}");
        Console.WriteLine($"Results: {Path.GetFullPath(ResultFolder)}");

        return added == 1 ? 0 : 2;
    }

    private static void ApplyLicense()
    {
        // Point this at your .lic file to remove evaluation limits.
        // Get a free temporary licence: https://purchase.groupdocs.com/temporary-license
        const string licensePath = "REPLACE_WITH_YOUR_LICENSE_PATH";
        if (File.Exists(licensePath))
        {
            new License().SetLicense(licensePath);
            Console.WriteLine("[license] applied");
        }
        else
        {
            Console.WriteLine("[license] no licence set - running in evaluation mode");
        }
    }

    /// <summary>
    /// Generates a page preview of a document with the default load settings.
    /// </summary>
    /// <remarks>
    /// Opens the document with <see cref="Signature"/> and no
    /// <see cref="LoadOptions"/>. Since GroupDocs.Signature 26.9,
    /// <see cref="LoadOptions.SkipExternalResources"/> is <c>true</c> by default:
    /// linked pictures, INCLUDEPICTURE fields, linked pictures in presentations and
    /// spreadsheets, and the images and style sheets of SVG files are not requested, so
    /// the preview shows an empty placeholder instead of the picture. Embedded pictures
    /// are not affected. This is the safe default for documents that come from users,
    /// e-mail or partner systems: a crafted document cannot make your server call
    /// internal addresses (server-side request forgery), leak Windows credentials
    /// through a UNC path, or wait for an unreachable host on an offline machine.
    /// Writes a PNG preview to <paramref name="previewPath"/> and returns its size in
    /// bytes.
    /// </remarks>
    public static long PreviewWithDefaultSettings(string sourcePath, string previewPath)
    {
        using var signature = new Signature(sourcePath);
        return SavePagePreview(signature, previewPath);
    }

    /// <summary>
    /// Generates a page preview that loads external resources from trusted addresses
    /// only.
    /// </summary>
    /// <remarks>
    /// Passes a <see cref="LoadOptions"/> with
    /// <see cref="LoadOptions.WhitelistedResources"/> to the <see cref="Signature"/>
    /// constructor. External resources stay skipped, except those whose address
    /// contains one of the listed fragments, compared ignoring case. Prefer long
    /// fragments such as a scheme, host and path: a short one like "github" would also
    /// match an attacker's address that merely contains that word. Use this when your
    /// documents legitimately link to a company CDN or an internal image server. Writes
    /// a PNG preview with the linked picture to <paramref name="previewPath"/> and
    /// returns its size in bytes. The machine needs access to the trusted host.
    /// </remarks>
    public static long PreviewWithWhitelistedHost(
        string sourcePath, string previewPath, string trustedAddress)
    {
        var loadOptions = new LoadOptions
        {
            WhitelistedResources = new List<string> { trustedAddress }
        };

        using var signature = new Signature(sourcePath, loadOptions);
        return SavePagePreview(signature, previewPath);
    }

    /// <summary>
    /// Generates a page preview that loads every external resource a document links to.
    /// </summary>
    /// <remarks>
    /// Sets <see cref="LoadOptions.SkipExternalResources"/> to <c>false</c>, which
    /// restores the behaviour of versions before 26.9: every linked picture and style
    /// sheet is requested while the document is loaded. Use it only for documents you
    /// trust, such as files your own application produced. The obsolete
    /// <c>LoadExternalResources</c> property has the opposite meaning:
    /// <c>SkipExternalResources = false</c> replaces <c>LoadExternalResources =
    /// true</c>. Writes a PNG preview to <paramref name="previewPath"/> and returns its
    /// size in bytes.
    /// </remarks>
    public static long PreviewWithAllExternalResources(
        string sourcePath, string previewPath)
    {
        var loadOptions = new LoadOptions { SkipExternalResources = false };

        using var signature = new Signature(sourcePath, loadOptions);
        return SavePagePreview(signature, previewPath);
    }

    /// <summary>
    /// Signs an untrusted Word document without fetching anything it links to.
    /// </summary>
    /// <remarks>
    /// Opens the document with the default settings and adds a QR-code signature
    /// through <see cref="QrCodeSignOptions"/> and <c>Sign</c>. No external resource is
    /// requested while the document is loaded, signed and saved, and the signed
    /// document keeps its link, so an application that opens it later can still show
    /// the picture. This is the typical server-side flow for signing files that users
    /// upload. Writes the signed DOCX to <paramref name="outputPath"/> and returns the
    /// number of signatures added.
    /// </remarks>
    public static int SignUntrustedDocument(string sourcePath, string outputPath)
    {
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
    }

    private static long SavePagePreview(Signature signature, string previewPath)
    {
        // The sample document has one page, so the page preview goes to one file.
        var previewOptions = new PreviewOptions(
            pageData => File.Create(previewPath),
            (pageData, pageStream) => pageStream.Dispose())
        {
            PreviewFormat = PreviewOptions.PreviewFormats.PNG
        };

        signature.GeneratePreview(previewOptions);
        return new FileInfo(previewPath).Length;
    }
}

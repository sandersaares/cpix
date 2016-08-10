Axinom CPIX library
===================

A .NET library for working with CPIX (Content Protection Information eXchange) documents.

Implemented CPIX version: 2.0 public review, with additional modifications predicted to be in 2.0 final.

Platform compatibility
======================

This library is compatible with .NET Framework 4.6.2 and newer.

Features
========

The following features are implemented:

* Content key save/load
* Usage rule save/load
* Resolving content keys based on usage rules
* Encryption of content keys (optional)
* Decryption of content keys
* Signing of content keys
* Signing of usage rules
* Signing of the document
* Automatic verification of all signatures
* Modification of existing document without having access to a decryption key
* Modification of existing document without invalidating signatures
* Automatic document validation against CPIX XML schema

The following features are NOT implemented:

* Key periods and key period filters
* DRM system metadata
* Document update history

Documents containing unimplemented features can still be processed - the unknown elements will simply be ignored on load and passed through without modification on save.

Quick start: writing CPIX
=========================

```C#
var document = new CpixDocument();

// Let's create a CPIX document with two content keys.
document.ContentKeys.Add(new ContentKey
{
	Id = Guid.NewGuid(),
	Value = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 1, 2, 3, 4, 5, 6 }
});
document.ContentKeys.Add(new ContentKey
{
	Id = Guid.NewGuid(),
	Value = new byte[] { 6, 7, 8, 9, 10, 1, 2, 3, 4, 5, 6, 1, 2, 3, 4, 5 }
});

// Optional: only needed to digitally sign the document.
var myCertificateAndPrivateKey = new X509Certificate2("Author1.pfx", "Author1");

// Here, we sign the list added elements to and also the document as a whole.
document.ContentKeys.AddSignature(myCertificateAndPrivateKey);
document.SignedBy = myCertificateAndPrivateKey;

// Optional: only needed to encrypt the content keys in the document.
var recipientCertificate = new X509Certificate2("Recipient1.cer");

// The presence of any recipients will automatically mark the content keys to be encrypted on save.
document.Recipients.Add(new Recipient(recipientCertificate));

using (var file = File.Create("cpix.xml"))
{
	document.Save(file);
}
```

Quick start: reading CPIX
=========================

```C#
// A suitable input document is the one generated by the "writing CPIX" quick start example.

// Optional: only needed to read encrypted content keys.
var myCertificateAndPrivateKey = new X509Certificate2("Recipient1.pfx", "Recipient1");

CpixDocument document;

using (var file = File.OpenRead("cpix.xml"))
{
	// Any private keys referenced by the certificate(s) you provide to Load() will be used for decrypting any
	// encrypted content keys. Even if you do not have a matching private key, the document will still be
	// successfully loaded but you will simply not have access to the values of the content keys.
	document = CpixDocument.Load(file, myCertificateAndPrivateKey);
}

if (document.ContentKeysAreReadable)
	Console.WriteLine("We have access to the content key values.");
else
	Console.WriteLine("The content keys are encrypted and we do not have a delivery key.");

var firstKey = document.ContentKeys.FirstOrDefault();
var firstSignerOfKeys = document.ContentKeys.SignedBy.FirstOrDefault();

if (firstKey != null)
	Console.WriteLine("First content key ID: " + firstKey.Id);
else
	Console.WriteLine("No content keys in document.");

if (firstSignerOfKeys != null)
	Console.WriteLine("Content keys first signed by: " + firstSignerOfKeys.SubjectName.Format(false));
else
	Console.WriteLine("The content keys collection was not signed.");

if (document.SignedBy != null)
	Console.WriteLine("Document signed by: " + document.SignedBy.SubjectName.Format(false));
else
	Console.WriteLine("The document as a whole was not signed.");
```

Quick start: modifying CPIX
=========================

```C#
// Scenario: we take an input document containing some content keys and define usage rules for those keys.
// A suitable input document is the one generated by the "writing CPIX" quick start example.

CpixDocument document;

using (var file = File.OpenRead("cpix.xml"))
	document = CpixDocument.Load(file);

if (document.ContentKeys.Count() < 2)
	throw new Exception("This sample assumes at least 2 content keys to be present in the CPIX document.");

// We are modifying the document, so we must first remove any document signature.
document.SignedBy = null;

// We are going to add some usage rules, so remove any signature on usage rules.
document.UsageRules.RemoveAllSignatures();

// If any usage rules already exist, get rid of them all.
document.UsageRules.Clear();

// Assign the first content key to all audio streams.
document.UsageRules.Add(new UsageRule
{
	KeyId = document.ContentKeys.First().Id,

	AudioFilters = new[] { new AudioFilter() }
});

// Assign the second content key to all video streams.
document.UsageRules.Add(new UsageRule
{
	KeyId = document.ContentKeys.Skip(1).First().Id,

	VideoFilters = new[] { new VideoFilter() }
});

// Save all changes. Note that we do not sign or re-sign anything in this example (although we could).

using (var file = File.Create("cpix.xml"))
{
	document.Save(file);
}
```

Quick start: mapping content keys to samples
============================================

```C#
// Scenario: we take a CPIX document with content keys and usage rules for audio and video.
// Then we map these content keys to audio and video samples that we want to encrypt.
// A suitable input document is the one generated by the "modifying CPIX" quick start example.

var myCertificateAndPrivateKey = new X509Certificate2("Recipient1.pfx", "Recipient1");

CpixDocument document;

using (var file = File.OpenRead("cpix.xml"))
	document = CpixDocument.Load(file, myCertificateAndPrivateKey);

if (!document.ContentKeysAreReadable)
	throw new Exception("The content keys were encrypted and we did not have a delivery key.");

// Let's imagine we have stereo audio at 32 kbps.
var audioKey = document.ResolveContentKey(new SampleDescription
{
	Type = SampleType.Audio,

	Bitrate = 32 * 1000,
	AudioChannelCount = 2
});

// Let's imagine we have both SD and HD video.
var sdVideoKey = document.ResolveContentKey(new SampleDescription
{
	Type = SampleType.Video,

	Bitrate = 1 * 1000 * 1000,
	PicturePixelCount = 640 * 480,
	WideColorGamut = false,
	HighDynamicRange = false,
	VideoFramesPerSecond = 30
});

var hdVideoKey = document.ResolveContentKey(new SampleDescription
{
	Type = SampleType.Video,

	Bitrate = 4 * 1000 * 1000,
	PicturePixelCount = 1920 * 1080,
	WideColorGamut = false,
	HighDynamicRange = false,
	VideoFramesPerSecond = 30
});
```
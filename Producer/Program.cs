﻿using Axinom.Cpix;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Producer
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("Loading certificates.");

			// If this is loaded from PFX, must be marked exportable due to funny behavior in .NET Framework.
			// Ideally, there should be no need to use an exportable key! But good enough for a sample.
			var signerCertificate1 = new X509Certificate2("Author1.pfx", "Author1", X509KeyStorageFlags.Exportable);
			var signerCertificate2 = new X509Certificate2("Author2.pfx", "Author2", X509KeyStorageFlags.Exportable);

			var recipientCertificate1 = new X509Certificate2("Recipient1.cer");
			var recipientCertificate2 = new X509Certificate2("Recipient2.cer");

			Console.WriteLine("Preparing data for sample documents.");

			// Tuples of filename and CpixDocument structure to generate.
			var samples = new List<Tuple<string, CpixDocument>>();

			#region Example: ClearKeys.xml
			var document = new CpixDocument();
			document.AddContentKey(GenerateNewKey());
			document.AddContentKey(GenerateNewKey());
			samples.Add(new Tuple<string, CpixDocument>("ClearKeys.xml", document));
			#endregion

			#region Example: Signed.xml
			document = new CpixDocument();
			document.AddContentKey(GenerateNewKey());
			document.AddContentKey(GenerateNewKey());
			document.AddContentKeySignature(signerCertificate1);
			document.SetDocumentSignature(signerCertificate1);
			samples.Add(new Tuple<string, CpixDocument>("Signed.xml", document));
			#endregion

			#region Example: Encrypted.xml
			document = new CpixDocument();
			document.AddContentKey(GenerateNewKey());
			document.AddContentKey(GenerateNewKey());
			document.AddRecipient(recipientCertificate1);
			document.AddRecipient(recipientCertificate2);
			samples.Add(new Tuple<string, CpixDocument>("Encrypted.xml", document));
			#endregion

			#region Example: EncryptedAndSigned.xml
			document = new CpixDocument();
			document.AddContentKey(GenerateNewKey());
			document.AddContentKey(GenerateNewKey());
			document.AddRecipient(recipientCertificate1);
			document.AddRecipient(recipientCertificate2);
			document.AddContentKeySignature(signerCertificate1);
			document.SetDocumentSignature(signerCertificate1);
			samples.Add(new Tuple<string, CpixDocument>("EncryptedAndSigned.xml", document));
			#endregion

			#region WithRulesAndEncryptedAndSigned.xml
			document = new CpixDocument();

			var lowValueKeyPeriod1 = GenerateNewKey();
			var highValueKeyPeriod1 = GenerateNewKey();
			var lowValueKeyPeriod2 = GenerateNewKey();
			var highValueKeyPeriod2 = GenerateNewKey();
			var audioKey = GenerateNewKey();

			var periodDuration = TimeSpan.FromHours(1);
			var period1Start = new DateTimeOffset(2016, 6, 6, 6, 10, 0, TimeSpan.Zero);
			var period2Start = period1Start + periodDuration;

			document.AddContentKey(lowValueKeyPeriod1);
			document.AddContentKey(highValueKeyPeriod1);
			document.AddContentKey(lowValueKeyPeriod2);
			document.AddContentKey(highValueKeyPeriod2);
			document.AddContentKey(audioKey);

			document.AddAssignmentRule(new AssignmentRule
			{
				KeyId = lowValueKeyPeriod1.Id,

				TimeFilter = new TimeFilter
				{
					Start = period1Start,
					End = period1Start + periodDuration
				},
				VideoFilter = new VideoFilter
				{
					MaxPixels = 1280 * 720 - 1
				}
			});

			document.AddAssignmentRule(new AssignmentRule
			{
				KeyId = lowValueKeyPeriod2.Id,

				TimeFilter = new TimeFilter
				{
					Start = period2Start,
					End = period2Start + periodDuration
				},
				VideoFilter = new VideoFilter
				{
					MaxPixels = 1280 * 720 - 1
				}
			});

			document.AddAssignmentRule(new AssignmentRule
			{
				KeyId = highValueKeyPeriod1.Id,

				TimeFilter = new TimeFilter
				{
					Start = period1Start,
					End = period1Start + periodDuration
				},
				VideoFilter = new VideoFilter
				{
					MinPixels = 1280 * 720
				}
			});

			document.AddAssignmentRule(new AssignmentRule
			{
				KeyId = highValueKeyPeriod2.Id,

				TimeFilter = new TimeFilter
				{
					Start = period2Start,
					End = period2Start + periodDuration
				},
				VideoFilter = new VideoFilter
				{
					MinPixels = 1280 * 720
				}
			});

			document.AddAssignmentRule(new AssignmentRule
			{
				KeyId = audioKey.Id,
				AudioFilter = new AudioFilter()
			});

			document.AddRecipient(recipientCertificate1);
			document.AddRecipient(recipientCertificate2);
			document.AddContentKeySignature(signerCertificate1);
			document.AddAssignmentRuleSignature(signerCertificate2);
			document.SetDocumentSignature(signerCertificate2);
			samples.Add(new Tuple<string, CpixDocument>("WithRulesAndEncryptedAndSigned.xml", document)); 
			#endregion

			Console.WriteLine("Saving CPIX documents.");

			foreach (var sample in samples)
			{
				Console.WriteLine(sample.Item1);
				Console.WriteLine();

				using (var file = File.Create(sample.Item1))
					sample.Item2.Save(file);

				Console.WriteLine(File.ReadAllText(sample.Item1));

				Console.WriteLine();
			}

			Console.WriteLine("All done.");
		}

		private static ContentKey GenerateNewKey()
		{
			var key = new byte[16];
			_random.GetBytes(key);

			return new ContentKey
			{
				Id = Guid.NewGuid(),
				Value = key
			};
		}

		private static RandomNumberGenerator _random = RandomNumberGenerator.Create();
	}
}

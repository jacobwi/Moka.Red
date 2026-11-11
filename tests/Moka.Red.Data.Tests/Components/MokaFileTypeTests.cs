using Moka.Red.Core.Enums;
using Moka.Red.Data.FileBrowser;

namespace Moka.Red.Data.Tests.Components;

public class MokaFileTypeTests
{
	[Theory]
	[InlineData("photo.PNG", MokaFileCategory.Image, "PNG", MokaColor.Success)]
	[InlineData("report.pdf", MokaFileCategory.Document, "PDF", MokaColor.Error)]
	[InlineData("notes.md", MokaFileCategory.Document, "MD", MokaColor.Info)]
	[InlineData("Program.cs", MokaFileCategory.Code, "CS", MokaColor.Info)]
	[InlineData("build.log", MokaFileCategory.Log, "LOG", null)]
	[InlineData("song.mp3", MokaFileCategory.Audio, "MP3", null)]
	[InlineData("clip.mkv", MokaFileCategory.Video, "MKV", null)]
	[InlineData("site.zip", MokaFileCategory.Archive, "ZIP", null)]
	[InlineData("thing.weird", MokaFileCategory.Other, "WEIRD", null)]
	public void FromName_ReadsTheExtension(string name, MokaFileCategory category, string tag, MokaColor? tone)
	{
		MokaFileType type = MokaFileType.FromName(name);

		Assert.Equal(new MokaFileType(category, tag, tone), type);
	}

	[Theory]
	[InlineData("Makefile")]
	[InlineData(".gitignore")]
	[InlineData("trailing.")]
	public void ANameWithoutAnExtension_IsOtherAndUntagged(string name) =>
		Assert.Equal(new MokaFileType(MokaFileCategory.Other), MokaFileType.FromName(name));

	[Fact]
	public void ALongExtension_KeepsItsCategory_ButNoTag()
	{
		MokaFileType type = MokaFileType.FromName("App.markdown");

		Assert.Equal(MokaFileCategory.Document, type.Category);
		Assert.Null(type.Tag);
	}

	[Fact]
	public void AFolder_IsTheWarningColor_WhateverItsName()
	{
		Assert.Equal(MokaFileType.Folder, MokaFileType.Of(new MokaFileInfo("/x", "photos.png", IsFolder: true)));
		Assert.Equal(MokaColor.Warning, MokaFileType.Folder.Tone);
	}
}

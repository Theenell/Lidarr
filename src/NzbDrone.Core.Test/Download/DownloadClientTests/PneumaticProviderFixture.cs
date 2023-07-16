using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using FizzWare.NBuilder;
using Moq;
using NLog;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Download;
using NzbDrone.Core.Download.Clients.Pneumatic;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Test.IndexerTests;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.Download.DownloadClientTests
{
    [TestFixture]
    public class PneumaticProviderFixture : CoreTest<Pneumatic>
    {
        private const string _nzbUrl = "http://www.nzbs.com/url";
        private const string _title = "30.Rock.S01E05.hdtv.xvid-LoL";
        private string _pneumaticFolder;
        private string _strmFolder;
        private string _nzbPath;
        private RemoteAlbum _remoteAlbum;
        private IIndexer _indexer;
        private DownloadClientItem _downloadClientItem;

        [SetUp]
        public void Setup()
        {
            _pneumaticFolder = @"d:\nzb\pneumatic\".AsOsAgnostic();

            _nzbPath = Path.Combine(_pneumaticFolder, _title + ".nzb").AsOsAgnostic();
            _strmFolder = @"d:\unsorted tv\".AsOsAgnostic();

            _remoteAlbum = new RemoteAlbum();
            _remoteAlbum.Release = new ReleaseInfo();
            _remoteAlbum.Release.Title = _title;
            _remoteAlbum.Release.DownloadUrl = _nzbUrl;

            _remoteAlbum.ParsedAlbumInfo = new ParsedAlbumInfo();

            _indexer = new TestIndexer(Mocker.Resolve<IHttpClient>(),
                Mocker.Resolve<IIndexerStatusService>(),
                Mocker.Resolve<IConfigService>(),
                Mocker.Resolve<IParsingService>(),
                Mocker.Resolve<Logger>());

            _downloadClientItem = Builder<DownloadClientItem>
                                  .CreateNew().With(d => d.DownloadId = "_Droned.S01E01.Pilot.1080p.WEB-DL-DRONE_0")
                                  .Build();

            Subject.Definition = new DownloadClientDefinition();
            Subject.Definition.Settings = new PneumaticSettings
            {
                NzbFolder = _pneumaticFolder,
                StrmFolder = _strmFolder
            };
        }

        private void WithFailedDownload()
        {
            Mocker.GetMock<IHttpClient>().Setup(c => c.DownloadFileAsync(It.IsAny<string>(), It.IsAny<string>())).Throws(new WebException());
        }

        [Test]
        public async Task should_download_file_if_it_doesnt_exist()
        {
            await Subject.Download(_remoteAlbum, _indexer);

            Mocker.GetMock<IHttpClient>().Verify(c => c.DownloadFileAsync(_nzbUrl, _nzbPath), Times.Once());
        }

        [Test]
        public void should_throw_on_failed_download()
        {
            WithFailedDownload();

            Assert.ThrowsAsync<WebException>(async () => await Subject.Download(_remoteAlbum, _indexer));
        }

        [Test]
        public void should_throw_if_discography_download()
        {
            _remoteAlbum.Release.Title = "Alien Ant Farm - Discography";
            _remoteAlbum.ParsedAlbumInfo.Discography = true;

            Assert.ThrowsAsync<NotSupportedException>(async () => await Subject.Download(_remoteAlbum, _indexer));
        }

        [Test]
        public void should_throw_item_is_removed()
        {
            Assert.Throws<NotSupportedException>(() => Subject.RemoveItem(_downloadClientItem, true));
        }

        [Test]
        public async Task should_replace_illegal_characters_in_title()
        {
            var illegalTitle = "Saturday Night Live - S38E08 - Jeremy Renner/Maroon 5 [SDTV]";
            var expectedFilename = Path.Combine(_pneumaticFolder, "Saturday Night Live - S38E08 - Jeremy Renner+Maroon 5 [SDTV].nzb");
            _remoteAlbum.Release.Title = illegalTitle;

            await Subject.Download(_remoteAlbum, _indexer);

            Mocker.GetMock<IHttpClient>().Verify(c => c.DownloadFileAsync(It.IsAny<string>(), expectedFilename), Times.Once());
        }
    }
}

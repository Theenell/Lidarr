using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Music;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Releases;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.DecisionEngineTests
{
    [TestFixture]
    public class ReleaseRestrictionsSpecificationFixture : CoreTest<ReleaseRestrictionsSpecification>
    {
        private RemoteAlbum _remoteAlbum;

        [SetUp]
        public void Setup()
        {
            _remoteAlbum = new RemoteAlbum
            {
                Artist = new Artist
                {
                    Tags = new HashSet<int>()
                },
                Release = new ReleaseInfo
                {
                    Title = "Dexter.S08E01.EDITED.WEBRip.x264-KYR"
                }
            };

            Mocker.SetConstant<ITermMatcherService>(Mocker.Resolve<TermMatcherService>());
        }

        private void GivenRestictions(List<string> required, List<string> ignored)
        {
            Mocker.GetMock<IReleaseProfileService>()
                  .Setup(s => s.EnabledForTags(It.IsAny<HashSet<int>>(), It.IsAny<int>()))
                  .Returns(new List<ReleaseProfile>
                           {
                               new ReleaseProfile()
                               {
                                   Required = required,
                                   Ignored = ignored
                               }
                           });
        }

        [Test]
        public void should_be_true_when_restrictions_are_empty()
        {
            Mocker.GetMock<IReleaseProfileService>()
                  .Setup(s => s.EnabledForTags(It.IsAny<HashSet<int>>(), It.IsAny<int>()))
                  .Returns(new List<ReleaseProfile>());

            Subject.IsSatisfiedBy(_remoteAlbum, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_be_true_when_title_contains_one_required_term()
        {
            GivenRestictions(new List<string> { "WEBRip" }, new List<string>());

            Subject.IsSatisfiedBy(_remoteAlbum, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_be_false_when_title_does_not_contain_any_required_terms()
        {
            GivenRestictions(new List<string> { "doesnt", "exist" }, new List<string>());

            Subject.IsSatisfiedBy(_remoteAlbum, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_be_true_when_title_does_not_contain_any_ignored_terms()
        {
            GivenRestictions(new List<string>(), new List<string> { "ignored" });

            Subject.IsSatisfiedBy(_remoteAlbum, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_be_false_when_title_contains_one_anded_ignored_terms()
        {
            GivenRestictions(new List<string>(), new List<string> { "edited" });

            Subject.IsSatisfiedBy(_remoteAlbum, null).Accepted.Should().BeFalse();
        }

        [TestCase("EdiTED")]
        [TestCase("webrip")]
        [TestCase("X264")]
        [TestCase("X264,NOTTHERE")]
        public void should_ignore_case_when_matching_required(string required)
        {
            GivenRestictions(required.Split(',').ToList(), new List<string>());

            Subject.IsSatisfiedBy(_remoteAlbum, null).Accepted.Should().BeTrue();
        }

        [TestCase("EdiTED")]
        [TestCase("webrip")]
        [TestCase("X264")]
        [TestCase("X264,NOTTHERE")]
        public void should_ignore_case_when_matching_ignored(string ignored)
        {
            GivenRestictions(new List<string>(), ignored.Split(',').ToList());

            Subject.IsSatisfiedBy(_remoteAlbum, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_be_false_when_release_contains_one_restricted_word_and_one_required_word()
        {
            _remoteAlbum.Release.Title = "[ www.Speed.cd ] - Katy Perry - Witness (2017) MP3 [320 kbps] ";

            Mocker.GetMock<IReleaseProfileService>()
                  .Setup(s => s.EnabledForTags(It.IsAny<HashSet<int>>(), It.IsAny<int>()))
                  .Returns(new List<ReleaseProfile>
                           {
                               new ReleaseProfile { Required = new List<string> { "320" }, Ignored = new List<string> { "www.Speed.cd" } }
                           });

            Subject.IsSatisfiedBy(_remoteAlbum, null).Accepted.Should().BeFalse();
        }

        [TestCase("/WEB/", true)]
        [TestCase("/WEB\b/", false)]
        [TestCase("/WEb/", false)]
        [TestCase(@"/\.WEB/", true)]
        public void should_match_perl_regex(string pattern, bool expected)
        {
            GivenRestictions(pattern.Split(',').ToList(), new List<string>());

            Subject.IsSatisfiedBy(_remoteAlbum, null).Accepted.Should().Be(expected);
        }
    }
}

using Czf.Wa.Initiatives.Dto;

namespace Czf.Wa.Initiatives.Test
{
    public class LiveTests
    {
        private HttpClient _httpClient;
        private InitiativeClient _client;

        [OneTimeSetUp]
        public void Setup()
        {
            _httpClient = new HttpClient();
            _client = new(_httpClient);

        }
       
        [OneTimeTearDown]
        public void TearDown() { _httpClient.Dispose(); }

        [Test]
        public async Task Test2022PeopleHeaders()
        {
            _client.Now = new DateTimeOffset(2022, 01, 02, 01, 01, 0, TimeSpan.Zero);
            var response = await _client.GetInitiativeToThePeopleHeaders();
            Assert.That(response, Is.Not.Null);
            Assert.IsNotEmpty(response);
            Assert.That(response, Has.Exactly(135).Items);
            CollectionAssert.AllItemsAreNotNull(response);
        }

        [Test]
        public async Task Test2004PeopleHeaders()
        {
            _client.Now = new DateTimeOffset(2004, 01, 02, 01, 01, 0, TimeSpan.Zero);
            var response = await _client.GetInitiativeToThePeopleHeaders();
            Assert.That(response, Is.Not.Null);
            Assert.IsEmpty(response);
        }

        [Test]
        public async Task Test2022LegislatureHeaders()
        {
            _client.Now = new DateTimeOffset(2022, 01, 02, 01, 01, 0, TimeSpan.Zero);
            var response = await _client.GetInitiativeToThePeopleHeaders();
            Assert.That(response, Is.Not.Null);
            Assert.IsNotEmpty(response);
            Assert.That(response, Has.Exactly(135).Items);
            CollectionAssert.AllItemsAreNotNull(response);
        }

        [Test]
        public async Task TestGetInitiative2468WithoutWebsite()
        {
            InitiativeHeader header = new InitiativeHeader(2468, true, DateTimeOffset.MinValue, string.Empty, 0, string.Empty, string.Empty);
            var initiative = await _client.GetInitiative(header);
            Assert.NotNull(initiative);
            Assert.IsNotNull(initiative.ballotTitleLetter);
            Assert.IsNotNull(initiative.completeText);
        }
        [Test]
        public async Task TestGetInitiative2451WithWebsite()
        {
            InitiativeHeader header = new InitiativeHeader(2451, true, DateTimeOffset.MinValue, string.Empty, 0, string.Empty, string.Empty);
            var initiative = await _client.GetInitiative(header);
            Assert.NotNull(initiative);
            Assert.IsNotNull(initiative.ballotTitleLetter);
            Assert.IsNotNull(initiative.completeText);
            Assert.IsNotNull(initiative.contactInformation.website);
        }
        [Test]
        public async Task TestGetInitiative2591WithoutAssetsSkipsAdditionalSponsors()
        {
            InitiativeHeader header = new InitiativeHeader(2591, false, DateTimeOffset.MinValue, string.Empty, 0, string.Empty, string.Empty);
            var initiative = await _client.GetInitiative(header);
            Assert.NotNull(initiative);
            Assert.NotNull(initiative.fullText);
            Assert.IsNull(initiative.ballotTitleLetter);
            Assert.IsNull(initiative.completeText);
        }
        [Test]
        public async Task TestGetInitiative2525WithOnlyCompleteTextAsset()
        {
            InitiativeHeader header = new InitiativeHeader(2525, false, DateTimeOffset.MinValue, string.Empty, 0, string.Empty, string.Empty);
            var initiative = await _client.GetInitiative(header);
            Assert.NotNull(initiative);
            Assert.Null(initiative.fullText);
            Assert.IsNull(initiative.ballotTitleLetter);
            Assert.IsNotNull(initiative.completeText);
        }
    }
}
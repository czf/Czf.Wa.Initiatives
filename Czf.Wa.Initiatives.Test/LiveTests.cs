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
        public async Task Test2022LegislatureHeaders()
        {
            _client.Now = new DateTimeOffset(2022, 01, 02, 01, 01, 0, TimeSpan.Zero);
            var response = await _client.GetInitiativeToThePeopleHeaders();
            Assert.That(response, Is.Not.Null);
            Assert.IsNotEmpty(response);
            Assert.That(response, Has.Exactly(135).Items);
            CollectionAssert.AllItemsAreNotNull(response);
        }
    }
}
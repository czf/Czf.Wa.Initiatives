using Czf.Wa.Initiatives.Dto;

namespace Czf.Wa.Initiatives;

public interface IInitiativeClient
{
    public Task<List<InitiativeHeader>> GetInitiativeToThePeopleHeaders();
    public Task<List<InitiativeHeader>> GetInitiativeToTheLegislatureHeaders();

    public Task<Initiative?> GetInitiative(InitiativeHeader initiativeHeader);
}

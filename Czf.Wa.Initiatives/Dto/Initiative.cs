namespace Czf.Wa.Initiatives.Dto;

public record InitiativeHeader(int id, bool flag, DateTimeOffset dateFiled, string fallbackDate, int assignedNumber, string primarySponsor, string subject);

public record Initiative(int id, DateTimeOffset dateFiled, string fallbackDate, int assignedNumber, string primarySponsor, string subject,
    ContactInfo contactInformation, string? ballotTitle, string? ballotSummary, Uri? ballotTitleLetter, Uri? completeText, string? fullText);
    

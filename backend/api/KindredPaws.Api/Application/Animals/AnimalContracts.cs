using KindredPaws.Api.Domain.Animals;

namespace KindredPaws.Api.Application.Animals;

public sealed record CreateShelterRequest(string Name, string Description, string Address, string City, string Country, string? Phone, string? WhatsApp, string? Email);
public sealed record ShelterResponse(Guid Id, string Name, string Description, string Address, string City, string Country, string? Phone, string? WhatsApp, string? Email, int AnimalCount);
public sealed record CreateAnimalRequest(Guid ShelterId, string Name, AnimalSpecies Species, AnimalSex Sex, AnimalSize Size, int? AgeMonths, string? Breed, string Description, string? Location);
public sealed record UpdateAnimalRequest(string Name, AnimalSpecies Species, AnimalSex Sex, AnimalSize Size, int? AgeMonths, string? Breed, string Description, string? Location, AdoptionStatus AdoptionStatus);
public sealed record AnimalResponse(Guid Id, Guid ShelterId, string ShelterName, string Name, AnimalSpecies Species, AnimalSex Sex, AnimalSize Size, int? AgeMonths, string? Breed, string Description, AdoptionStatus AdoptionStatus, string? Location, IReadOnlyCollection<AnimalMediaResponse> Media);
public sealed record AnimalMediaResponse(Guid Id, string Url, string? ThumbnailUrl, string ContentType, bool IsPrimary);
public sealed record MediaUpload(string FileName, string ContentType, long Length, Stream Content, bool IsPrimary);
public sealed record AnimalStatsResponse(Guid AnimalId, string AnimalName, AdoptionStatus AdoptionStatus, int PostCount, int TotalLikes, int TotalComments, int TotalViews, int TotalShares, int FollowerCount);

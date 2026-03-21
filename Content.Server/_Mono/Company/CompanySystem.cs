using Content.Shared._Mono.Company;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.GameTicking;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Server.Audio; // Forge-change: spawnSound
using Robust.Shared.Audio; // Forge-change: spawnSound

namespace Content.Server._Mono.Company;

/// <summary>
/// This system handles assigning a company to players when they join.
/// TODO: remove hardcoded slop.
/// whoever hardcoded ts is getting slimed out no joke.
/// </summary>
public sealed class CompanySystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedJobSystem _job = default!;
    [Dependency] private readonly SharedIdCardSystem _idCardSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly AudioSystem _audio = default!; // Forge-change: SpawnSound

    // Dictionary to store original company preferences for players
    private readonly Dictionary<string, string> _playerOriginalCompanies = new();

    private readonly HashSet<string> _tsfJobs = new()
    {
        "Sheriff",
        "Bailiff",
        "SeniorOfficer", // Sergeant
        "Deputy",
        "Brigmedic",
        "NFDetective",
        "PublicAffairsLiaison",
        "Cadet",
        "TsfEngineer",
        "TsfBorg",
        // Forge-change-start
        "TsfCommandingOfficer",
        "TsfExecutiveOfficer",
        "TsfSeniorOfficer",
        "TsfSeniorAide",
        "TsfAmbassador",
        "TsfRanger",
        "TsfRecruit",
        "TsfEngineer",
        // Forge-change-end
    };

    private readonly HashSet<string> _rogues = new()
    {
        "PirateCaptain",
        "PirateFirstMate",
        "Pirate",
        "PDVDenasvar",
        "PDVInfiltrator",
        "PdvBorg",
    };

    private readonly HashSet<string> _imperial = new()
    {
        "Praefect",
        "Arbiter",
        "Cardinal",
        "Inquisitor",
        "Consul",
        "Praetorian",
        "Auxilia",
        "Neophyte",
    };

    private readonly HashSet<string> _renegates = new()
    {
        "Baron",
        "Draftsman",
        "Overseer",
        "Quack",
        "Foreman",
        "Flunky",
    };

    // private readonly HashSet<string> _usspJobs = new()
    // {
    //    "USSPCommissar",
    //    "USSPSergeant",
    //    "USSPCorporal",
    //    "USSPMedic",
    //    "USSPRifleman"
    //};

    private readonly HashSet<string> _colonialJobs = new()
    {
        "StationRepresentative",
        "StationTrafficController",
        "SecurityGuard",
        "NFJanitor",
        "MailCarrier",
        "Valet",
    };

    private readonly HashSet<string> _mdJobs = new()
    {
        "DirectorOfCare",
        "MdMedic",
    };

    public override void Initialize()
    {
        base.Initialize();

        // Subscribe to player spawn event to add the company component
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);

        // Subscribe to player detached event to clean up stored preferences
        SubscribeLocalEvent<PlayerDetachedEvent>(OnPlayerDetached);
    }

    private void OnPlayerDetached(PlayerDetachedEvent args)
    {
        // Clean up stored preferences when player disconnects
        _playerOriginalCompanies.Remove(args.Player.UserId.ToString());
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        // Add the company component with the player's saved company
        var companyComp = EnsureComp<Shared._Mono.Company.CompanyComponent>(args.Mob);

        var playerId = args.Player.UserId.ToString();
        var profileCompany = args.Profile.Company;

        // Store the player's original company preference if not already stored
        if (!_playerOriginalCompanies.ContainsKey(playerId))
        {
            _playerOriginalCompanies[playerId] = profileCompany;
        }

        // todo - make this a switch statement or something lol. who cares.
        // Check if player's job is one of the TSF jobs
        if (args.JobId != null && _tsfJobs.Contains(args.JobId))
        {
            // Assign TSF company
            companyComp.CompanyName = "TSF";
        }
        // Check if player's job is one of the Rogue jobs
        else if (args.JobId != null && _rogues.Contains(args.JobId))
        {
            // Assign Rogue company
            companyComp.CompanyName = "PDV";
        }
        // Check if player's job is one of the USSP jobs
        //else if (args.JobId != null && _usspJobs.Contains(args.JobId))
        //{
        //    // Assign USSP company
        //    companyComp.CompanyName = "USSP";
        //}
        else if (args.JobId != null && _colonialJobs.Contains(args.JobId))
        {
            // Assign MD company
            companyComp.CompanyName = "Nanotrasen"; // Forge-change: Colonial to NT
        }
        else if (args.JobId != null && _mdJobs.Contains(args.JobId))
        {
            // Assign MD company
            companyComp.CompanyName = "Hospital"; // Forge-change: MD to Hospital
        }
        // Forge-change: add imperial and renegates
        else if (args.JobId != null && _imperial.Contains(args.JobId))
        {
            companyComp.CompanyName = "Imperial";
        }
        else if (args.JobId != null && _renegates.Contains(args.JobId))
        {
            companyComp.CompanyName = "None";
        }
        else
        {
            // Only consider whitelist if the player has NO specific company preference
            bool loginFound = false;

            // Only check logins if the player hasn't explicitly set a company preference
            // or if their preference is "None"
            if (string.IsNullOrEmpty(profileCompany))
            {
                // Check for company login whitelists
                foreach (var companyProto in _prototypeManager.EnumeratePrototypes<CompanyPrototype>())
                {
                    if (companyProto.Logins.Contains(args.Player.Name))
                    {
                        companyComp.CompanyName = companyProto.ID;
                        loginFound = true;
                        break;
                    }
                }
            }

            // If no login was found or login check was skipped due to player preference, use the player's preference
            if (!loginFound)
            {
                // Use "None" as fallback for empty company
                if (string.IsNullOrEmpty(profileCompany))
                    profileCompany = "None";

                // Restore the player's original company preference
                companyComp.CompanyName = profileCompany;
            }
        }

        // Forge-change-start
        if (_prototypeManager.TryIndex<CompanyPrototype>(companyComp.CompanyName, out var proto))
        {
            if (proto.SpawnSound != null)
            {
                var audioParams = AudioParams.Default.WithVolume(-5f);
                _audio.PlayPvs(proto.SpawnSound, args.Mob, audioParams);
            }
        }
        // Forge-change-end

        // Ensure the component is networked to clients
        Dirty(args.Mob, companyComp);

        // Update the player's ID card with the company information
        UpdateIdCardCompany(args.Mob, companyComp.CompanyName);
    }

    /// <summary>
    /// Updates the player's ID card with their company information
    /// </summary>
    private void UpdateIdCardCompany(EntityUid playerEntity, string companyName)
    {
        // Try to get the player's ID card
        if (!_inventorySystem.TryGetSlotEntity(playerEntity, "id", out var idUid))
            return;

        var cardId = idUid.Value;

        // Check if it's a PDA with an ID card inside
        if (TryComp<PdaComponent>(idUid, out var pdaComponent) && pdaComponent.ContainedId != null)
            cardId = pdaComponent.ContainedId.Value;

        // Update the ID card with company information
        if (TryComp<IdCardComponent>(cardId, out var idCard))
        {
            _idCardSystem.TryChangeCompanyName(cardId, companyName, idCard);
        }
    }
}

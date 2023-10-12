﻿using CommunityToolkit.Mvvm.ComponentModel;
using Wayfinder.Services;
using Wayfinder.Services.Challenges;
using Wayfinder.Services.Models;

namespace Wayfinder.App.Features.RequiredUnits;

public partial class RequiredUnitsViewModel : ObservableObject
{
    [ObservableProperty] List<Challenge> _selectedChallenges = default!;
    [ObservableProperty] List<Unit> _selectedUnits = default!;
    [ObservableProperty] private List<RequiredUnit> _requirements = default!;
    [ObservableProperty] private List<Challenge> _challenges = default!;

    public IReadOnlyList<Unit> AllUnits { get; set; } = default!;
    protected List<Challenge> AllChallenges { get; set; } = default!;

    partial void OnSelectedChallengesChanged(List<Challenge> value)
    {
        LoadRequiredUnits();
    }

    public virtual async Task InitializeAsync()
    {
        await LoadAllUnitsAsync();
        await LoadAllChallengesAsync();

        Challenges ??= AllChallenges;

        var query = from c in Challenges
                    from r in c.Requirements
                    join u in AllUnits on r.UnitId equals u.BaseId
                    orderby u.Name
                    select u;

        SelectedUnits = query.ToList();

        LoadRequiredUnits();
    }

    protected async Task LoadAllUnitsAsync() => AllUnits = await GameService.GetUnitsAsync();

    protected virtual async Task LoadAllChallengesAsync() => AllChallenges = await ChallengeService.GetAsync();

    //internal static async Task<List<Challenge>> GetAllChallengesAsync() => await ChallengeService.GetAsync();

    public virtual void LoadRequiredUnits()
    {
        var selectedUnitIds = SelectedUnits.Select(x => x.BaseId).ToArray();
        var objectives = AllChallenges.Where(x => selectedUnitIds.Contains(x.ChallengeId));
        var requirements = objectives.SelectMany(x => x.Requirements).ToList();

        var unitDict = AllUnits.ToDictionary(x => x.BaseId, x => x.Name);

        var query = from g in objectives
                    from r in g.Requirements
                    select new { GoalUnit = g.ChallengeId, RequiredUnit = r.UnitId, RequiredLevel = r.Level } into d
                    group d by d.RequiredUnit into ru
                    select new RequiredUnit(ru.Key, ru.Select(x => new RequiredDetail(x.GoalUnit, x.RequiredLevel)).ToList())                    ;

        Requirements = query.ToList();
    }
}

public record RequiredDetail(string GoalUnitId, string Level);

public record RequiredUnit(string UnitId, List<RequiredDetail> Details);
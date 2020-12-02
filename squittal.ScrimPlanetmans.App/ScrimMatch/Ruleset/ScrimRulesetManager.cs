﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using squittal.ScrimPlanetmans.Data;
using squittal.ScrimPlanetmans.ScrimMatch.Messages;
using squittal.ScrimPlanetmans.ScrimMatch.Models;
using squittal.ScrimPlanetmans.Services.Planetside;
using squittal.ScrimPlanetmans.Services.Rulesets;
using squittal.ScrimPlanetmans.Services.ScrimMatch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace squittal.ScrimPlanetmans.ScrimMatch
{
    public class ScrimRulesetManager : IScrimRulesetManager
    {
        private readonly IDbContextHelper _dbContextHelper;
        private readonly IItemCategoryService _itemCategoryService;
        private readonly IItemService _itemService;
        private readonly IRulesetDataService _rulesetDataService;
        private readonly IScrimMessageBroadcastService _messageService;
        public ILogger<ScrimRulesetManager> _logger;

        public Ruleset ActiveRuleset { get; private set; }

        private readonly int _defaultRulesetId = 1;

        private readonly AutoResetEvent _activateRulesetAutoEvent = new AutoResetEvent(true);

        public ScrimRulesetManager(IDbContextHelper dbContextHelper, IItemCategoryService itemCategoryService, IItemService itemService, IRulesetDataService rulesetDataService, IScrimMessageBroadcastService messageService, ILogger<ScrimRulesetManager> logger)
        {
            _dbContextHelper = dbContextHelper;
            _itemCategoryService = itemCategoryService;
            _itemService = itemService;
            _rulesetDataService = rulesetDataService;
            _messageService = messageService;
            _logger = logger;

            _messageService.RaiseRulesetRuleChangeEvent += async (s, e) => await HandleRulesetRuleChangeMesssage(s, e);
        }

        public async Task<IEnumerable<Ruleset>> GetRulesetsAsync(CancellationToken cancellationToken)
        {
            return await _rulesetDataService.GetAllRulesetsAsync(cancellationToken);
        }

        public async Task<Ruleset> GetActiveRulesetAsync(bool forceRefresh = false)
        {
            if (ActiveRuleset == null)
            {
                return await ActivateDefaultRulesetAsync();
            }
            else if (forceRefresh || ActiveRuleset.RulesetActionRules == null || !ActiveRuleset.RulesetActionRules.Any() || ActiveRuleset.RulesetItemCategoryRules == null || !ActiveRuleset.RulesetItemCategoryRules.Any())
            {
                await SetUpActiveRulesetAsync();
                return ActiveRuleset;
            }
            else
            {
                return ActiveRuleset;
            }
        }


        public async Task<Ruleset> ActivateRulesetAsync(int rulesetId)
        {
            _activateRulesetAutoEvent.WaitOne();

            try
            {
                using var factory = _dbContextHelper.GetFactory();
                var dbContext = factory.GetDbContext();

                Ruleset currentActiveRuleset = null;
                
                if (ActiveRuleset != null)
                {
                    currentActiveRuleset = ActiveRuleset;
                
                    if (rulesetId == currentActiveRuleset.Id)
                    {
                        _activateRulesetAutoEvent.Set();

                        return currentActiveRuleset;
                    }
                }

                var newActiveRuleset = await _rulesetDataService.GetRulesetFromIdAsync(rulesetId, CancellationToken.None);

                if (newActiveRuleset == null)
                {
                    _activateRulesetAutoEvent.Set();
                    
                    return null;
                }

                _rulesetDataService.SetActiveRulesetId(rulesetId);

                ActiveRuleset = newActiveRuleset;

                var message = currentActiveRuleset == null
                                    ? new ActiveRulesetChangeMessage(ActiveRuleset)
                                    : new ActiveRulesetChangeMessage(ActiveRuleset, currentActiveRuleset);

                _messageService.BroadcastActiveRulesetChangeMessage(message);

                _activateRulesetAutoEvent.Set();

                return ActiveRuleset;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());

                _activateRulesetAutoEvent.Set();

                return null;
            }
        }

        public async Task<Ruleset> ActivateDefaultRulesetAsync()
        {
            using var factory = _dbContextHelper.GetFactory();
            var dbContext = factory.GetDbContext();

            var ruleset = await dbContext.Rulesets.FirstOrDefaultAsync(r => r.IsCustomDefault);

            if (ruleset == null)
            {
                _logger.LogInformation($"No custom default ruleset found. Loading default ruleset...");
                ruleset = await dbContext.Rulesets.FirstOrDefaultAsync(r => r.IsDefault);
            }

            if (ruleset == null)
            {
                _logger.LogError($"Failed to activate default ruleset: no ruleset found");
                return null;
            }

            ActiveRuleset = await ActivateRulesetAsync(ruleset.Id);

            _logger.LogInformation($"Active ruleset loaded: {ActiveRuleset.Name}");

            return ActiveRuleset;
        }

        public async Task SetUpActiveRulesetAsync()
        {
            _activateRulesetAutoEvent.WaitOne();

            try
            {
                using var factory = _dbContextHelper.GetFactory();
                var dbContext = factory.GetDbContext();

                var currentActiveRuleset = ActiveRuleset;

                if (currentActiveRuleset == null)
                {
                    _logger.LogError($"Failed to set up active ruleset: no ruleset found");

                    _activateRulesetAutoEvent.Set();

                    return;
                }

                var tempRuleset = await _rulesetDataService.GetRulesetFromIdAsync(currentActiveRuleset.Id, CancellationToken.None);

                if (tempRuleset == null)
                {
                    _logger.LogError($"Failed to set up active ruleset: temp ruleset is null");

                    _activateRulesetAutoEvent.Set();

                    return;
                }

                ActiveRuleset = tempRuleset;

                _rulesetDataService.SetActiveRulesetId(ActiveRuleset.Id);

                _logger.LogInformation($"Active ruleset collections loaded: {ActiveRuleset.Name}");

                _activateRulesetAutoEvent.Set();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to set up active ruleset: {ex}");

                _activateRulesetAutoEvent.Set();
            }
        }

        private async Task HandleRulesetRuleChangeMesssage(object sender, ScrimMessageEventArgs<RulesetRuleChangeMessage> e)
        {
            var changedRulesetId = e.Message.Ruleset.Id;

            if (changedRulesetId == ActiveRuleset.Id)
            {
                await SetUpActiveRulesetAsync();
            }
        }

        public async Task<Ruleset> GetDefaultRulesetAsync()
        {
            using var factory = _dbContextHelper.GetFactory();
            var dbContext = factory.GetDbContext();

            var ruleset = await dbContext.Rulesets.FirstOrDefaultAsync(r => r.IsDefault);

            if (ruleset == null)
            {
                return null;
            }

            ruleset = await _rulesetDataService.GetRulesetFromIdAsync(ruleset.Id, CancellationToken.None);

            return ruleset;
        }

        public async Task SeedDefaultRuleset()
        {
            using var factory = _dbContextHelper.GetFactory();
            var dbContext = factory.GetDbContext();

            var defaultRulesetId = _defaultRulesetId;

            var storeRuleset = await dbContext.Rulesets.FirstOrDefaultAsync(r => r.Id == defaultRulesetId);

            bool rulesetExistsInDb = false;

            var storeActionRules = new List<RulesetActionRule>();
            var storeItemCategoryRules = new List<RulesetItemCategoryRule>();
            var storeItemRules = new List<RulesetItemRule>();
            var storeFacilityRules = new List<RulesetFacilityRule>();

            if (storeRuleset != null)
            {
                // TODO: use Task.WhenAll for this 

                storeActionRules = await dbContext.RulesetActionRules.Where(r => r.RulesetId == storeRuleset.Id).ToListAsync();

                storeItemCategoryRules = await dbContext.RulesetItemCategoryRules.Where(r => r.RulesetId == storeRuleset.Id).ToListAsync();

                storeItemRules = await dbContext.RulesetItemRules.Where(r => r.RulesetId == storeRuleset.Id).ToListAsync();

                storeFacilityRules = await dbContext.RulesetFacilityRules.Where(r => r.RulesetId == storeRuleset.Id).ToListAsync();

                rulesetExistsInDb = true;
            }
            else
            {
                var utcNow = DateTime.UtcNow;
                var newRuleset = new Ruleset
                {
                    Name = "Default",
                    DateCreated = utcNow
                };

                storeRuleset = newRuleset;
            }

            storeRuleset.DefaultMatchTitle = "PS2 Scrims";
            storeRuleset.IsDefault = true;

            #region Action rules
            var defaultActionRules = GetDefaultActionRules();
            var createdActionRules = new List<RulesetActionRule>();
            var allActionRules = new List<RulesetActionRule>();

            var allActionEnumValues = GetScrimActionTypes().Where(a => a != ScrimActionType.None && a != ScrimActionType.Login && a != ScrimActionType.Logout);

            var allActionValues = new List<ScrimActionType>();
            allActionValues.AddRange(allActionEnumValues);
            allActionValues.AddRange(storeActionRules.Select(ar => ar.ScrimActionType).Where(a => !allActionValues.Contains(a)).ToList());

            foreach (var actionType in allActionValues)
            {
                var storeEntity = storeActionRules?.FirstOrDefault(r => r.ScrimActionType == actionType);
                var defaultEntity = defaultActionRules.FirstOrDefault(r => r.ScrimActionType == actionType);

                var isValidAction = storeEntity == null || allActionEnumValues.Any(enumValue => enumValue == storeEntity.ScrimActionType);

                if (storeEntity == null)
                {
                    if (defaultEntity != null)
                    {
                        defaultEntity.RulesetId = defaultRulesetId;
                        createdActionRules.Add(defaultEntity);
                        allActionRules.Add(defaultEntity);
                    }
                    else
                    {
                        var newEntity = BuildRulesetActionRule(defaultRulesetId, actionType, 0);
                        createdActionRules.Add(newEntity);
                        allActionRules.Add(newEntity);
                    }
                }
                else if (isValidAction)
                {
                    if (defaultEntity != null)
                    {
                        storeEntity.Points = defaultEntity.Points;
                        storeEntity.DeferToItemCategoryRules = defaultEntity.DeferToItemCategoryRules;
                        storeEntity.ScrimActionTypeDomain = defaultEntity.ScrimActionTypeDomain;
                    }
                    else
                    {
                        storeEntity.Points = 0;
                        storeEntity.ScrimActionTypeDomain = ScrimAction.GetDomainFromActionType(storeEntity.ScrimActionType);
                    }

                    dbContext.RulesetActionRules.Update(storeEntity);
                    allActionRules.Add(storeEntity);
                }
                else
                {
                    dbContext.RulesetActionRules.Remove(storeEntity);
                }
            }

            if (createdActionRules.Any())
            {
                //await dbContext.RulesetActionRules.AddRangeAsync(createdActionRules);
                dbContext.RulesetActionRules.AddRange(createdActionRules);
            }
            #endregion Action rules

            #region Item Category Rules
            var defaultItemCategoryRules = GetDefaultItemCategoryRules();
            var createdItemCategoryRules = new List<RulesetItemCategoryRule>();
            var allItemCategoryIds = await _itemCategoryService.GetItemCategoryIdsAsync();
            var allWeaponItemCategoryIds = await _itemCategoryService.GetWeaponItemCategoryIdsAsync();

            var allItemCategoryRules = new List<RulesetItemCategoryRule>();

            foreach (var categoryId in allItemCategoryIds)
            {
                var isWeaponItemCategoryId = (allWeaponItemCategoryIds.Contains(categoryId));

                var storeEntity = storeItemCategoryRules?.FirstOrDefault(r => r.ItemCategoryId == categoryId);
                var defaultEntity = defaultItemCategoryRules.FirstOrDefault(r => r.ItemCategoryId == categoryId);

                if (storeEntity == null)
                {
                    if (defaultEntity != null)
                    {
                        defaultEntity.RulesetId = defaultRulesetId;

                        createdItemCategoryRules.Add(defaultEntity);
                        allItemCategoryRules.Add(defaultEntity);
                    }
                    else if (isWeaponItemCategoryId)
                    {
                        var newEntity = BuildRulesetItemCategoryRule(defaultRulesetId, categoryId, 0);
                        createdItemCategoryRules.Add(newEntity);
                        allItemCategoryRules.Add(newEntity);

                    }
                }
                else
                {
                    if (isWeaponItemCategoryId)
                    {
                        storeEntity.Points = defaultEntity != null ? defaultEntity.Points : 0;
                        storeEntity.IsBanned = defaultEntity != null ? defaultEntity.IsBanned : false;
                        storeEntity.DeferToItemRules = defaultEntity != null ? defaultEntity.DeferToItemRules : false;

                        dbContext.RulesetItemCategoryRules.Update(storeEntity);
                        allItemCategoryRules.Add(storeEntity);
                    }
                    else
                    {
                        dbContext.RulesetItemCategoryRules.Remove(storeEntity);
                    }
                }
            }

            if (createdItemCategoryRules.Any())
            {
                //await dbContext.RulesetItemCategoryRules.AddRangeAsync(createdItemCategoryRules);
                dbContext.RulesetItemCategoryRules.AddRange(createdItemCategoryRules);
            }
            #endregion Item Category Rules

            #region Item Rules
            var defaultItemRules = GetDefaultItemRules();
            var createdItemRules = new List<RulesetItemRule>();
            //var allItemIds = await _itemService.GetItemIdsAsync();
            var allWeaponItems = await _itemService.GetAllWeaponItemsAsync();

            var allItemIds = new List<int>(defaultItemRules.Select(r => r.ItemId));
            if (storeItemRules != null)
            {
                allItemIds.AddRange(storeItemRules.Where(r => !allItemIds.Contains(r.ItemId)).Select(r => r.ItemId));
            }

            if (allWeaponItems != null)
            {
                allItemIds.AddRange(allWeaponItems.Where(r => !allItemIds.Contains(r.Id)).Select(r => r.Id));
            }

            var allItemRules = new List<RulesetItemRule>();

            foreach (var itemId in allItemIds)
            {
                var isWeaponItem = (allWeaponItems.Any(r => r.Id == itemId));

                var storeEntity = storeItemRules?.FirstOrDefault(r => r.ItemId == itemId);
                var defaultEntity = defaultItemRules.FirstOrDefault(r => r.ItemId == itemId);

                if (storeEntity == null)
                {
                    if (defaultEntity != null)
                    {
                        defaultEntity.RulesetId = defaultRulesetId;

                        createdItemRules.Add(defaultEntity);
                        allItemRules.Add(defaultEntity);
                    }
                    else if (isWeaponItem)
                    {
                        var categoryId = allWeaponItems.Where(i => i.Id == itemId).Select(i => i.ItemCategoryId).FirstOrDefault();

                        if (categoryId != null)
                        {
                            var defaultPoints = allItemCategoryRules.Where(r => r.ItemCategoryId == categoryId).Select(r => r.Points).FirstOrDefault();

                            var newEntity = BuildRulesetItemRule(defaultRulesetId, itemId, (int)categoryId, defaultPoints, false);
                            
                            createdItemRules.Add(newEntity);
                            allItemRules.Add(newEntity);
                        }    
                    }
                }
                else
                {
                    var categoryId = allWeaponItems.Where(i => i.Id == itemId).Select(i => i.ItemCategoryId).FirstOrDefault();

                    if (isWeaponItem && categoryId != null)
                    {
                        storeEntity.Points = defaultEntity != null
                                                ? defaultEntity.Points
                                                : allItemCategoryRules.Where(r => r.ItemCategoryId == categoryId).Select(r => r.Points).FirstOrDefault(); ;

                        storeEntity.IsBanned = defaultEntity != null ? defaultEntity.IsBanned : false;

                        dbContext.RulesetItemRules.Update(storeEntity);
                        allItemRules.Add(storeEntity);
                    }
                    else
                    {
                        dbContext.RulesetItemRules.Remove(storeEntity);
                    }
                }
            }

            if (createdItemRules.Any())
            {
                //await dbContext.RulesetItemRules.AddRangeAsync(createdItemRules);
                dbContext.RulesetItemRules.AddRange(createdItemRules);
            }
            #endregion Item Rules

            #region Facility Rules
            var defaultFacilityRules = GetDefaultFacilityRules();

            var createdFacilityRules = new List<RulesetFacilityRule>();

            var allFacilityRules = new List<RulesetFacilityRule>(storeFacilityRules);
            allFacilityRules.AddRange(defaultFacilityRules.Where(d => !allFacilityRules.Any(a => a.FacilityId == d.FacilityId)));

            foreach (var facilityRule in allFacilityRules)
            {
                var storeEntity = storeFacilityRules?.FirstOrDefault(r => r.FacilityId == facilityRule.FacilityId);
                var defaultEntity = defaultFacilityRules.FirstOrDefault(r => r.FacilityId == facilityRule.FacilityId);

                if (storeEntity == null)
                {
                    if (defaultEntity != null)
                    {
                        defaultEntity.RulesetId = defaultRulesetId;

                        createdFacilityRules.Add(defaultEntity);

                    }
                }
                else
                {
                    if (defaultEntity == null)
                    {
                        dbContext.RulesetFacilityRules.Remove(storeEntity);
                        allFacilityRules.Remove(storeEntity);
                    }
                    else
                    {
                        //storeEntity = defaultEntity;
                        //dbContext.RulesetFacilityRules.Update(storeEntity);
                    }
                }
            }

            if (createdFacilityRules.Any())
            {
                //await dbContext.RulesetFacilityRules.AddRangeAsync(createdFacilityRules);
                dbContext.RulesetFacilityRules.AddRange(createdFacilityRules);
            }
            #endregion Facility Rules


            storeRuleset.RulesetActionRules = allActionRules;
            storeRuleset.RulesetItemCategoryRules = allItemCategoryRules;
            storeRuleset.RulesetItemRules = allItemRules;
            storeRuleset.RulesetFacilityRules = allFacilityRules;

            if (rulesetExistsInDb)
            {
                dbContext.Rulesets.Update(storeRuleset);
            }
            else
            {
                dbContext.Rulesets.Add(storeRuleset);
            }

            await dbContext.SaveChangesAsync();
        }

        private IEnumerable<RulesetItemCategoryRule> GetDefaultItemCategoryRules()
        {
            return new RulesetItemCategoryRule[]
            {
                BuildRulesetItemCategoryRule(2, 1, false, true),   // Knife
                BuildRulesetItemCategoryRule(3, 1, false, false),   // Pistol
                BuildRulesetItemCategoryRule(5, 1, false, false),   // SMG
                BuildRulesetItemCategoryRule(6, 1, false, false),   // LMG
                BuildRulesetItemCategoryRule(7, 1, false, false),   // Assault Rifle
                BuildRulesetItemCategoryRule(8, 1, false, false),   // Carbine
                BuildRulesetItemCategoryRule(11, 1, false, false),  // Sniper Rifle
                BuildRulesetItemCategoryRule(12, 1, false, false),  // Scout Rifle
                BuildRulesetItemCategoryRule(19, 1, false, false),  // Battle Rifle
                BuildRulesetItemCategoryRule(24, 1, false, false),  // Crossbow
                BuildRulesetItemCategoryRule(100, 1, false, false), // Infantry
                BuildRulesetItemCategoryRule(102, 1, false, false), // Infantry Weapons
                BuildRulesetItemCategoryRule(157, 1, false, false)  // Hybrid Rifle
            };
        }

        private IEnumerable<RulesetItemRule> GetDefaultItemRules()
        {
            return new RulesetItemRule[]
            {
                BuildRulesetItemRule(271, 2, 0, true),  // Carver
                BuildRulesetItemRule(285, 2, 0, true), // Ripper
                BuildRulesetItemRule(286, 2, 0, true)  // Lumine Edge
            };
        }

        private IEnumerable<RulesetActionRule> GetDefaultActionRules()
        {
            // MaxKillInfantry & MaxKillMax are worth 0 points
            return new RulesetActionRule[]
            {
                BuildRulesetActionRule(ScrimActionType.FirstBaseCapture, 9), // PIL 1: 18
                BuildRulesetActionRule(ScrimActionType.SubsequentBaseCapture, 18), // PIL 1: 36 
                BuildRulesetActionRule(ScrimActionType.InfantryKillMax, 6), // PIL 1: -12
                BuildRulesetActionRule(ScrimActionType.InfantryTeamkillInfantry, -2), // PIL 1: -3
                BuildRulesetActionRule(ScrimActionType.InfantryTeamkillMax, -8), // PIL 1: -15
                BuildRulesetActionRule(ScrimActionType.InfantrySuicide, -2), // PIL 1: -3
                BuildRulesetActionRule(ScrimActionType.MaxTeamkillMax, -8), // PIL 1: -15
                BuildRulesetActionRule(ScrimActionType.MaxTeamkillInfantry, -2), // PIL 1: -3
                BuildRulesetActionRule(ScrimActionType.MaxSuicide, -8), // PIL 1: -12
                BuildRulesetActionRule(ScrimActionType.MaxKillInfantry, 0), // PIL 1: 0
                BuildRulesetActionRule(ScrimActionType.MaxKillMax, 0), // PIL 1: 0
                BuildRulesetActionRule(ScrimActionType.InfantryKillInfantry, 0, true) // PIL 1: 0
            };
        }

        private IEnumerable<RulesetFacilityRule> GetDefaultFacilityRules()
        {
            return new RulesetFacilityRule[]
            {
                /* Hossin */
                BuildRulesetFacilityRule(266000, 4106), // Kessel's Crossing
                BuildRulesetFacilityRule(272000, 4112), // Bridgewater Shipping
                BuildRulesetFacilityRule(283000, 4123), // Nettlemire
                BuildRulesetFacilityRule(286000, 4126), // Four Fingers
                BuildRulesetFacilityRule(287070, 4266), // Fort Liberty
                BuildRulesetFacilityRule(302030, 4173), // Acan South
                BuildRulesetFacilityRule(303030, 4183), // Bitol Eastern
                BuildRulesetFacilityRule(305010, 4201), // Ghanan South
                BuildRulesetFacilityRule(307010, 4221), // Chac Fusion
                
                /* Esamir */
                BuildRulesetFacilityRule(239000, 18010), // Pale Canyon
                BuildRulesetFacilityRule(244610, 18067), // Rime Analtyics
                BuildRulesetFacilityRule(244620, 18068), // The Rink
                BuildRulesetFacilityRule(252020, 18050), // Elli Barracks
                BuildRulesetFacilityRule(254010, 18055), // Eisa Mountain Pass
                
                /* Indar */
                BuildRulesetFacilityRule(219, 2420), // Ceres
                BuildRulesetFacilityRule(230, 2431), // Xenotech
                BuildRulesetFacilityRule(3430, 2456), // Peris Eastern
                BuildRulesetFacilityRule(3620, 2466), // Rashnu
                
                /* Amerish */
                BuildRulesetFacilityRule(210002, 6357) // Wokuk Shipping
            };
        }

        private RulesetActionRule BuildRulesetActionRule(int rulesetId, ScrimActionType actionType, int points = 0, bool deferToItemCategoryRules = false)
        {
            return new RulesetActionRule
            {
                RulesetId = rulesetId,
                ScrimActionType = actionType,
                Points = points,
                DeferToItemCategoryRules = deferToItemCategoryRules,
                ScrimActionTypeDomain = ScrimAction.GetDomainFromActionType(actionType)
            };
        }

        private RulesetActionRule BuildRulesetActionRule(ScrimActionType actionType, int points = 0, bool deferToItemCategoryRules = false)
        {
            return new RulesetActionRule
            {
                ScrimActionType = actionType,
                Points = points,
                DeferToItemCategoryRules = deferToItemCategoryRules,
                ScrimActionTypeDomain = ScrimAction.GetDomainFromActionType(actionType)
            };
        }

        private RulesetItemCategoryRule BuildRulesetItemCategoryRule(int rulesetId, int itemCategoryId, int points = 0, bool isBanned = false, bool deferToItemRules = false)
        {
            return new RulesetItemCategoryRule
            {
                RulesetId = rulesetId,
                ItemCategoryId = itemCategoryId,
                Points = points,
                IsBanned = isBanned,
                DeferToItemRules = deferToItemRules
            };
        }

        private RulesetItemCategoryRule BuildRulesetItemCategoryRule(int itemCategoryId, int points = 0, bool isBanned = false, bool deferToItemRules = false)
        {
            return new RulesetItemCategoryRule
            {
                ItemCategoryId = itemCategoryId,
                Points = points,
                IsBanned = isBanned,
                DeferToItemRules = deferToItemRules
            };
        }

        private RulesetItemRule BuildRulesetItemRule(int rulesetId, int itemId, int itemCategoryId, int points = 0, bool isBanned = false)
        {
            return new RulesetItemRule
            {
                RulesetId = rulesetId,
                ItemId = itemId,
                ItemCategoryId = itemCategoryId,
                Points = points,
                IsBanned = isBanned
            };
        }

        private RulesetItemRule BuildRulesetItemRule(int itemId, int itemCategoryId, int points = 0, bool isBanned = false)
        {
            return new RulesetItemRule
            {
                ItemId = itemId,
                ItemCategoryId = itemCategoryId,
                Points = points,
                IsBanned = isBanned
            };
        }

        private RulesetFacilityRule BuildRulesetFacilityRule(int rulesetId, int facilityId, int mapRegionId)
        {
            return new RulesetFacilityRule
            {
                RulesetId = rulesetId,
                FacilityId = facilityId,
                MapRegionId = mapRegionId
            };
        }
        private RulesetFacilityRule BuildRulesetFacilityRule(int facilityId, int mapRegionId)
        {
            return new RulesetFacilityRule
            {
                FacilityId = facilityId,
                MapRegionId = mapRegionId
            };
        }

        public async Task SeedScrimActionModels()
        {
            using var factory = _dbContextHelper.GetFactory();
            var dbContext = factory.GetDbContext();

            var createdEntities = new List<ScrimAction>();

            var allActionTypeValues = new List<ScrimActionType>();

            var enumValues = (ScrimActionType[])Enum.GetValues(typeof(ScrimActionType));

            allActionTypeValues.AddRange(enumValues);

            var storeEntities = await dbContext.ScrimActions.ToListAsync();

            allActionTypeValues.AddRange(storeEntities.Where(a => !allActionTypeValues.Contains(a.Action)).Select(a => a.Action).ToList());

            allActionTypeValues.Distinct().ToList();

            foreach (var value in allActionTypeValues)
            {
                try
                {

                    var storeEntity = storeEntities.FirstOrDefault(e => e.Action == value);
                    var isValidEnum = enumValues.Any(enumValue => enumValue == value);

                    if (storeEntity == null)
                    {
                        createdEntities.Add(ConvertToDbModel(value));
                    }
                    else if (isValidEnum)
                    {
                        storeEntity = ConvertToDbModel(value);
                        dbContext.ScrimActions.Update(storeEntity);
                    }
                    else
                    {
                        dbContext.ScrimActions.Remove(storeEntity);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.ToString());
                }
            }

            if (createdEntities.Any())
            {
                await dbContext.ScrimActions.AddRangeAsync(createdEntities);
            }

            await dbContext.SaveChangesAsync();

            _logger.LogInformation($"Seeded Scrim Actions store");
        }

        private ScrimAction ConvertToDbModel(ScrimActionType value)
        {
            var name = Enum.GetName(typeof(ScrimActionType), value);

            return new ScrimAction
            {
                Action = value,
                Name = name,
                Description = Regex.Replace(name, @"(\p{Ll})(\p{Lu})", "$1 $2"),
                Domain = ScrimAction.GetDomainFromActionType(value)
            };
        }

        public IEnumerable<ScrimActionType> GetScrimActionTypes()
        {
            return (ScrimActionType[])Enum.GetValues(typeof(ScrimActionType));
        }
    }
}

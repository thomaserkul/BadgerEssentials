﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

namespace BadgerEssentialsServer
{
    public class BadgerEssentialsServer : BaseScript
    {
        string jsonConfig = LoadResourceFile("BadgerEssentials", "config/config.json");

        string currentAOP = "Sandy Shores test";
        bool peacetime = false;
        string currentPriorityStatus = "none";
        int priorityTime = 0;

        bool priorityTimerActive = false;
        public BadgerEssentialsServer()
        {
            Tick += OnTickPriorityTimer;

            JObject o = JObject.Parse(jsonConfig);

            currentAOP = (string)o.SelectToken("displayElements.aop.defaultAOP");
            //
            // Event Listeners
            //

            EventHandlers["BadgerEssentials:GetAOPFromServer"] += new Action<int>(SendAOP);

            //
            // Commands
            //\

            // Revive Command
            RegisterCommand("revive", new Action<int, List<object>, string>((source, args, raw) =>
            {

                // Revive Self 
                if (args.Count == 0 || int.Parse(args[0].ToString()) == source)
                    if (IsPlayerAceAllowed(source.ToString(), "BadgerEssentials.Bypass.ReviveTimer"))
                        TriggerClientEvent("BadgerEssentials:RevivePlayer", source, true, true);
                    else
                        TriggerClientEvent("BadgerEssentials:RevivePlayer", source, true, false);
                // Revive other person 
                else if (int.Parse(args[0].ToString()) != source)
                {
                    string playerName = GetPlayerName(args[0].ToString());
                    if (!string.IsNullOrEmpty(playerName))
                        TriggerClientEvent("BadgerEssentials:RevivePlayer", int.Parse(args[0].ToString()), false, false); ;
                }
            }), false);

            // Announcement Command
            RegisterCommand("announce", new Action<int, List<object>, string>((source, args, raw) =>
            {
                if (IsPlayerAceAllowed(source.ToString(), "BadgerEssentials.Command.Announce") && args.Count > 0)
				{
                    string announcementMsg = String.Join(" ", args);
                    TriggerClientEvent("BadgerEssentials:Announce", -1, announcementMsg);
				}
            }), false);

            // Priority Cooldown
            RegisterCommand("pc", new Action<int, List<object>, string>((source, args, raw) =>
            {
                if (IsPlayerAceAllowed(source.ToString(), "BadgerEssentials.Command.PriorityCooldown") && args.Count > 0 && args[0].ToString() != "0")
                {
                    currentPriorityStatus = "pc";
                    priorityTime = int.Parse(args[0].ToString());
                    TriggerClientEvent("BadgerEssentials:PriorityCooldown", "pc", priorityTime);
                }
            }), false);
            // Priority Cooldown in progress
            RegisterCommand("pc-inprogress", new Action<int, List<object>, string>((source, args, raw) =>
            {
                if (IsPlayerAceAllowed(source.ToString(), "BadgerEssentials.Command.PriorityCooldown"))
                {
                    currentPriorityStatus = "inprogress";
                    priorityTime = 0;
                    TriggerClientEvent("BadgerEssentials:PriorityCooldown", "inprogress", 0);
                }
            }), false);
            // Priority Cooldown on hold
            RegisterCommand("pc-onhold", new Action<int, List<object>, string>((source, args, raw) =>
            {
                if (IsPlayerAceAllowed(source.ToString(), "BadgerEssentials.Command.PriorityCooldown"))
                {
                    currentPriorityStatus = "onhold";
                    priorityTime = 0;
                    TriggerClientEvent("BadgerEssentials:PriorityCooldown", "onhold", 0);
                }
            }), false);
            // Priority Cooldown reset
            RegisterCommand("pc-reset", new Action<int, List<object>, string>((source, args, raw) =>
            {
                if (IsPlayerAceAllowed(source.ToString(), "BadgerEssentials.Command.PriorityCooldown"))
                {
                    currentPriorityStatus = "reset";
                    priorityTime = 0;
                    TriggerClientEvent("BadgerEssentials:PriorityCooldown", "reset", 0);
                }
            }), false);

            // Toggle Peacetime
            RegisterCommand("pt", new Action<int, List<object>, string>((source, args, raw) =>
            {
                if (IsPlayerAceAllowed(source.ToString(), "BadgerEssentials.Command.Peacetime"))
                {
                    if (peacetime)
                        peacetime = false;
                    else peacetime = true;

                    TriggerClientEvent("BadgerEssentials:Peacetime", peacetime);
                }
            }), false);

            // Set AOP
            RegisterCommand("setAOP", new Action<int, List<object>, string>((source, args, raw) =>
            {
                if (args.Count > 0 && IsPlayerAceAllowed(source.ToString(), "BadgerEssentials.Command.SetAOP"))
                {
                    string targetAOP = String.Join(" ", args);
                    currentAOP = targetAOP;
                    TriggerClientEvent("BadgerEssentials:SetAOP", targetAOP);
                }
            }), false);
        }

        // Receive AOP / Peacetime / PC status from server
        private void SendAOP(int source)
		{
            TriggerClientEvent("BadgerEssentials:SendAOPToClient", currentAOP, peacetime, currentPriorityStatus, priorityTime);
        }

        private async Task OnTickPriorityTimer()
        {
            if (!priorityTimerActive && priorityTime > 0)
                priorityTimerActive = true;
            else if (priorityTimerActive && priorityTime > 0)
            {
                await Delay(60000);
                priorityTime--;
                if (priorityTime > 0)
                {
                    // Update remaining time for client
                    TriggerClientEvent("BadgerEssentials:PriorityCooldown", "pc", priorityTime);
                }
                else
                {
                    // Disable PC for client
                    TriggerClientEvent("BadgerEssentials:Prioritycooldown", "none", priorityTime);
                }
            }
            else if (priorityTimerActive && priorityTime <= 0)
            {
                priorityTimerActive = false;
                currentPriorityStatus = "none";
            }
        }
    }
}

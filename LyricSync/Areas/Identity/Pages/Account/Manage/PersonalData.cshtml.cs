// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using LyricSync.Models;
using Microsoft.AspNetCore.Authorization;

namespace LyricSync.Areas.Identity.Pages.Account.Manage
{
    [Authorize]
    public class PersonalDataModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<PersonalDataModel> _logger;

        public PersonalDataModel(
            UserManager<ApplicationUser> userManager,
            ILogger<PersonalDataModel> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> OnGet()
        {
            _logger.LogDebug("IsAuthenticated={IsAuthenticated}", User?.Identity?.IsAuthenticated);
            var principalUserId = _userManager.GetUserId(User);
            _logger.LogDebug("Principal user id from claims: {UserId}", principalUserId);

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("GetUserAsync returned null for principal id {UserId}", principalUserId);
                return NotFound($"Unable to load user with ID '{principalUserId}'.");
            }

            // successful: you can populate view data here
            return Page();
        }
    }
}

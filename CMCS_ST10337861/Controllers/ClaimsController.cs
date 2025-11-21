using CMCS_ST10337861.Data;
using CMCS_ST10337861.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CMCS_ST10337861.Controllers
{
    [Authorize]
    public class ClaimsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ClaimsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // LECTURER: Submit claim form
        [Authorize(Roles = "Lecturer")]
        public IActionResult Submit()
        {
            ViewBag.Months = new SelectList(new[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" });
            return View(new Claim { Year = DateTime.Now.Year });
        }

        // LECTURER: Save claim
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Lecturer")]
        public async Task<IActionResult> Submit(Claim model, IFormFile[] files)
        {
            ViewBag.Months = new SelectList(new[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" });

            // Ensure we have the logged-in user and set LecturerId before validation
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "User not found. Please log in again.");
                return View(model);
            }

            model.LecturerId = user.Id;

            // Clear any existing ModelState entry for LecturerId so validation uses this value
            ModelState.Remove(nameof(model.LecturerId));

            if (!TryValidateModel(model))
            {
                return View(model);
            }

            model.Status = "Pending";
            model.SubmittedDate = DateTime.Now;
            model.TotalAmount = model.HoursWorked * model.HourlyRate;

            // Save claim first
            _context.Claims.Add(model);
            await _context.SaveChangesAsync();

            // Upload files and attach supporting documents
            var uploadPath = Path.Combine(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads");
            Directory.CreateDirectory(uploadPath);

            if (files != null && files.Any(f => f?.Length > 0))
            {
                foreach (var file in files.Where(f => f != null && f.Length > 0))
                {
                    var safeFileName = Path.GetFileName(file.FileName);
                    var fileName = Guid.NewGuid() + "_" + safeFileName;
                    var fullPath = Path.Combine(uploadPath, fileName);
                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    _context.SupportingDocuments.Add(new SupportingDocument
                    {
                        ClaimId = model.ClaimId,
                        FileName = safeFileName,
                        FilePath = "/uploads/" + fileName
                    });
                }
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Claim submitted successfully!";
            return RedirectToAction("MyClaims");
        }

        [Authorize(Roles = "Lecturer,ProgrammeCoordinator,AcademicManager")]
        public async Task<IActionResult> Submitted(int id)
        {
            return RedirectToAction("MyClaims");
        }

        // LECTURER: My Claims
        [Authorize(Roles = "Lecturer")]
        public async Task<IActionResult> MyClaims()
        {
            var userId = _userManager.GetUserId(User);
            var claims = await _context.Claims
                .Include(c => c.SupportingDocuments)
                .Where(c => c.LecturerId == userId)
                .OrderByDescending(c => c.SubmittedDate)
                .ToListAsync();

            return View(claims);
        }

        // COORDINATOR
        [Authorize(Roles = "ProgrammeCoordinator")]
        public async Task<IActionResult> CoordinatorPending()
        {
            var claims = await _context.Claims
                .Include(c => c.Lecturer)
                .Include(c => c.SupportingDocuments)
                .Where(c => c.Status == "Pending")
                .OrderByDescending(c => c.SubmittedDate)
                .ToListAsync();
            return View(claims);
        }

        [HttpPost]
        [Authorize(Roles = "ProgrammeCoordinator")]
        public async Task<IActionResult> Verify(int id, string action)
        {
            var claim = await _context.Claims.FindAsync(id);
            if (claim != null)
            {
                claim.Status = action == "Approve" ? "Verified" : "Rejected";
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("CoordinatorPending");
        }

        // MANAGER
        [Authorize(Roles = "AcademicManager")]
        public async Task<IActionResult> ManagerPending()
        {
            var claims = await _context.Claims
                .Include(c => c.Lecturer)                  // This fixes the view errors
                .Include(c => c.SupportingDocuments)
                .Where(c => c.Status == "Verified")
                .OrderByDescending(c => c.SubmittedDate)
                .ToListAsync();
            return View(claims);
        }

        [HttpPost]
        [Authorize(Roles = "AcademicManager")]
        public async Task<IActionResult> Approve(int id, string action)
        {
            var claim = await _context.Claims.FindAsync(id);
            if (claim != null)
            {
                claim.Status = action == "Approve" ? "Approved" : "Rejected";
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("ManagerPending");
        }
    }
}
﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using AdaWebApp.Models.DAL;
using AdaWebApp.Models.Entities;
using AdaWebApp.Models.DAL.Repositories;
using Common.Logging;
using System.Threading.Tasks;

namespace AdaWebApp.Controllers.API
{
    [RoutePrefix("api/Visits")]
    public class VisitsController : ApiController
    {
        //ATTENTION A éliminer! Le PUT et le POST utilisent encore le DBContext
        //private ApplicationDbContext db = new ApplicationDbContext();
        private UnitOfWork _unit;
        private readonly ILog _logger;

        public VisitsController()
        {
            _unit = new UnitOfWork();

            log4net.Config.XmlConfigurator.Configure();
            _logger = LogManager.GetLogger(GetType());
        }

        // GET: api/Visits
        public async Task<IEnumerable<Visit>> GetVisits()
        {
           return await _unit.VisitsRepository.GetAll();
        }

        [HttpGet]
        [Route("visitsToday")]
        // GET: get visits of the day
        public List<Visit> GetVisitsToday()
        {
            return _unit.VisitsRepository.GetVisitsByDate();
        }

        // GET: api/Visits/5
        [ResponseType(typeof(Visit))]
        public async Task<IHttpActionResult> GetVisit(int id)
        {
            //Visit visit = db.Visits.Find(id);
            Visit visit = await _unit.VisitsRepository.GetByIdAsync(id);
            if (visit == null)
            {
                return NotFound();
            }

            return Ok(visit);
        }

        /*
        // PUT: api/Visits/5
        [ResponseType(typeof(void))]
        public IHttpActionResult PutVisit(int id, Visit visit)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != visit.Id)
            {
                return BadRequest();
            }

            db.Entry(visit).State = EntityState.Modified;

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!VisitExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return StatusCode(HttpStatusCode.NoContent);
        }

        // POST: api/Visits
        [ResponseType(typeof(Visit))]
        public IHttpActionResult PostVisit(Visit visit)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            //_unit.VisitsRepository.
            db.Visits.Add(visit);
            db.SaveChanges();

            return CreatedAtRoute("DefaultApi", new { id = visit.Id }, visit);
        }
        */

        // DELETE: api/Visits/5
        [ResponseType(typeof(Visit))]
        //public IHttpActionResult DeleteVisit(int id)
        public async Task<IHttpActionResult> DeleteVisit(int id)
        {
            //Visit visit = db.Visits.Find(id);
            Visit visit = await _unit.VisitsRepository.GetByIdAsync(id);
            if (visit == null)
            {
                return NotFound();
            }

            await _unit.VisitsRepository.Remove(id);
            await _unit.SaveAsync();
            //db.Visits.Remove(visit);
            //db.SaveChanges();

            return Ok(visit);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                //db.Dispose();
                _unit.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool VisitExists(int id)
        {
            //return db.Visits.Count(e => e.Id == id) > 0;
            return _unit.VisitsRepository.CheckVisitExist(id);
        }
    }
}
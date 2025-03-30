using Microsoft.EntityFrameworkCore;
using Personalblog.Model;
using Personalblog.Model.Entitys;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersonalblogServices.FPhoto
{
    public class FPhotoService : IFPhotoService
    {
        private readonly MyDbContext _myDbContext;
        public FPhotoService(MyDbContext myDbContext)
        {
            _myDbContext = myDbContext;
        }

        public Task<List<Photo>> GetFeaturePhotosAsync()
        {
            return _myDbContext.featuredPhotos.Include("Photo").Select(x => x.Photo).ToListAsync();
        }
    }
}

using System;
using XHTD_SERVICES.Data.Entities;

namespace XHTD_SERVICES.Data.Repositories
{
    public class AttachmentRepository : BaseRepository<tblAttachment>
    {
        public AttachmentRepository(XHTD_Entities appDbContext) : base(appDbContext)
        {
        }

        public int Create(tblAttachment attachment)
        {
            try
            {
                _appDbContext.tblAttachments.Add(attachment);
                _appDbContext.SaveChanges();
                return attachment.Id;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Add attachment fail: {ex.Message}");
                return 0;
            }
        }
    }
}

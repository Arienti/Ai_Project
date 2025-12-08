using Ai_Project.Database;
using Ai_Project.DTO;
using ModelsDTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ai_Project.Business
{
    public class MessagesBusiness
    {
        MessagesDB messagesDB;
        public MessagesBusiness()
        {
            if (messagesDB == null) messagesDB = new MessagesDB();
        }
        public async Task<ResultDTO> Insert(MessagesDTO messageDTO)
        {
            try
            {
                if (messageDTO == null)
                    return ResultDTO.Fail("messageDTO is null");

                if (string.IsNullOrWhiteSpace(messageDTO.Content))
                    return ResultDTO.Fail("Content is null or empty");

                messageDTO.CreatedAt = DateTime.UtcNow;
                return await messagesDB.Insert(messageDTO);
            }
            catch (Exception e)
            {
                return ResultDTO.Fail(e.InnerException?.Message);
            }
        }

        public async Task<ResultDTO> Update(MessagesDTO messageDTO)
        {
            try
            {
                if (messageDTO == null)
                    return ResultDTO.Fail("messageDTO is null");
                if (string.IsNullOrWhiteSpace(messageDTO.Content))
                    return ResultDTO.Fail("Content is null or empty");
                if (messageDTO.ID <= 0)
                    return ResultDTO.Fail("Invalid Message ID");
                return await messagesDB.Update(messageDTO);
            }
            catch (Exception e)
            {
                return ResultDTO.Fail(e.InnerException?.Message);
            }
        }

        public async Task<List<MessagesDTO>?> GetMessagesByTopicId(uint id)
        {
            ResultDTO result = await messagesDB.GetAllByTopicId(id);
            if (result != null && result.Data != null)
            {
                return result.Data as List<MessagesDTO>;
            }
            return null;
        }
    }
}

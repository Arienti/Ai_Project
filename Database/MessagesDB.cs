using Ai_Project.DTO;
using Microsoft.EntityFrameworkCore;
using ModelsDTO;

namespace Ai_Project.Database
{
    public class MessagesDB
    {
        public async Task<ResultDTO> Insert(MessagesDTO messageDTO)
        {
            try
            {
                if (messageDTO == null)
                {
                    return ResultDTO.Fail("messageDTO is null");
                }
                using (DataBase db = new DataBase())
                {
                    if (db.Messages != null)
                    {
                        db.Messages.Add(messageDTO);
                        await db.SaveChangesAsync();
                        return ResultDTO.Success(messageDTO);
                    }
                }
                return ResultDTO.Fail($"DataBase not connected.");
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
                {
                    return ResultDTO.Fail("messageDTO is null");
                }
                using (DataBase db = new DataBase())
                {
                    if (db.Messages != null)
                    {
                        var existingMessage = await db.Messages.FindAsync(messageDTO.ID);
                        if (existingMessage == null)
                        {
                            return ResultDTO.Fail($"Message with ID {messageDTO.ID} not found.");
                        }
                        existingMessage.Content = messageDTO.Content;
                        // Update other fields as necessary
                        db.Messages.Update(existingMessage);
                        await db.SaveChangesAsync();
                        return ResultDTO.Success(existingMessage);
                    }
                }
                return ResultDTO.Fail($"DataBase not connected.");
            }
            catch (Exception e)
            {
                return ResultDTO.Fail(e.InnerException?.Message);
            }
        }
        public async Task<ResultDTO> Delete(uint id)
        {
            try
            {
                if (id <= 0)
                {
                    return ResultDTO.Fail("Invalid Message ID");
                }
                using (DataBase db = new DataBase())
                {
                    if (db.Messages != null)
                    {
                        var existingMessage = await db.Messages.FindAsync(id);
                        if (existingMessage == null)
                        {
                            return ResultDTO.Fail($"Message with ID {id} not found.");
                        }
                        db.Messages.Remove(existingMessage);
                        await db.SaveChangesAsync();
                        return ResultDTO.Success();
                    }
                }
                return ResultDTO.Fail($"DataBase not connected.");
            }
            catch (Exception e)
            {
                return ResultDTO.Fail(e.InnerException?.Message);
            }
        }
        public async Task<ResultDTO> GetAllByTopicId(uint topicId)
        {
            try
            {
                if (topicId <= 0)
                {
                    return ResultDTO.Fail("Invalid Topic ID");
                }
                using (DataBase db = new DataBase())
                {
                    if (db.Messages != null)
                    {
                        var messages = await db.Messages
                            .Where(m => m.TopicId == topicId)
                            .OrderBy(x => x.CreatedAt)
                            .ToListAsync();
                        return ResultDTO.Success(messages);
                    }
                }
                return ResultDTO.Fail($"DataBase not connected.");
            }
            catch (Exception e)
            {
                return ResultDTO.Fail(e.InnerException?.Message);
            }
        }
    }
}

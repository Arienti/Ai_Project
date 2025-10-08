using Ai_Project.DTO;
using Microsoft.EntityFrameworkCore;

namespace Ai_Project.Database
{
    public class TopicDB
    {
        public async Task<ResultDTO> Insert(TopicDTO topicDTO)
        {
            try
            {
                if (topicDTO == null)
                {
                    return ResultDTO.Fail("client is null");
                }
                using (DataBase db = new DataBase())
                {
                    if (db.Topics != null)
                    {
                        db.Topics.Add(topicDTO);
                        await db.SaveChangesAsync();
                        return ResultDTO.Success(topicDTO);
                    }
                }
                return ResultDTO.Fail($"DataBase not connected.");
            }
            catch (Exception e)
            {
                return ResultDTO.Fail(e.InnerException?.Message);
            }
        }

        public async Task<ResultDTO> Update(TopicDTO topicDTO)
        {
            try
            {
                if (topicDTO == null)
                {
                    return ResultDTO.Fail("client is null");
                }
                using (DataBase db = new DataBase())
                {
                    if (db.Topics != null)
                    {
                        var existingTopic = await db.Topics.FindAsync(topicDTO.ID);
                        if (existingTopic == null)
                        {
                            return ResultDTO.Fail($"Topic with ID {topicDTO.ID} not found.");
                        }
                        existingTopic.Topic = topicDTO.Topic;
                        // Update other fields as necessary
                        db.Topics.Update(existingTopic);
                        await db.SaveChangesAsync();
                        return ResultDTO.Success(existingTopic);
                    }
                }
                return ResultDTO.Fail($"DataBase not connected.");
            }
            catch (Exception e)
            {
                return ResultDTO.Fail(e.InnerException?.Message);
            }
        }
        public async Task<ResultDTO> Delete(int id)
        {
            try
            {
                using (DataBase db = new DataBase())
                {
                    if (db.Topics != null)
                    {
                        var topic = await db.Topics.FindAsync(id);
                        if (topic == null)
                        {
                            return ResultDTO.Fail($"Topic with ID {id} not found.");
                        }
                        db.Topics.Remove(topic);
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

        public async Task<ResultDTO> GetById(int id)
        {
            try
            {
                using (DataBase db = new DataBase())
                {
                    if (db.Topics != null)
                    {
                        var topic = await db.Topics.FindAsync(id);
                        if (topic == null)
                        {
                            return ResultDTO.Fail($"Topic with ID {id} not found.");
                        }
                        return ResultDTO.Success(topic);
                    }
                }
                return ResultDTO.Fail($"DataBase not connected.");
            }
            catch (Exception e)
            {
                return ResultDTO.Fail(e.InnerException?.Message);
            }
        }
        public async Task<ResultDTO> GetAll()
        {
            try
            {
                using (DataBase db = new DataBase())
                {
                    if (db.Topics != null)
                    {
                        var topics = await db.Topics.ToListAsync();
                        return ResultDTO.Success(topics);
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

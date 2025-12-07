using Ai_Project.DTO;
using Microsoft.EntityFrameworkCore;
using ModelsDTO;

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
                        existingTopic.isFavorite = topicDTO.isFavorite;

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
        public async Task<ResultDTO> Delete(uint id)
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
                        var topic = await db.Topics
                            .Where(t => t.ID == id)
                            .Include(t => t.Messages.Where(m => m.TopicId == id)) // Filtered include
                            .FirstOrDefaultAsync();

                        if (topic == null)
                        {
                            return ResultDTO.Fail($"Topic with ID {id} not found.");
                        }
                        var filteredTopic = new TopicDTO
                        {
                            CreatedAt = topic.CreatedAt,
                            Topic = topic.Topic.Trim(),
                            ID = topic.ID,
                            Messages = topic.Messages
                        };
                        return ResultDTO.Success(filteredTopic);
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
                        var topics = await db.Topics.OrderByDescending(x => x.CreatedAt).ToListAsync();
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

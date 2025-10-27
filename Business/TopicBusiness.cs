using Ai_Project.Database;
using Ai_Project.DTO;

namespace Ai_Project.Business
{
    public class TopicBusiness
    {
        TopicDB topicsDB;
        public TopicBusiness()
        {
            if (topicsDB == null) topicsDB = new TopicDB();
        }
        public async Task<ResultDTO> Insert(TopicDTO topicDTO)
        {
            if (topicDTO == null)
                return ResultDTO.Fail("topicDTO is null");

            if (string.IsNullOrWhiteSpace(topicDTO.Topic))
                return ResultDTO.Fail("Topic is null or empty");

            try
            {
                topicDTO.CreatedAt = DateTime.UtcNow;
                return await topicsDB.Insert(topicDTO);
            }
            catch (Exception ex)
            {
                // log exception here if you want
                return ResultDTO.Fail($"Error inserting topic: {ex.Message}");
            }
        }

        public async Task<ResultDTO> Update(DTO.TopicDTO topicDTO)
        {
            if (topicDTO == null)
            {
                return ResultDTO.Fail("topicDTO is null");
            }
            if (string.IsNullOrWhiteSpace(topicDTO.Topic))
            {
                return ResultDTO.Fail("Topic is null or empty");
            }
            if (topicDTO.ID <= 0)
            {
                return ResultDTO.Fail("Invalid Topic ID");
            }
            try
            {
                return await topicsDB.Update(topicDTO);
            }
            catch (Exception ex)
            {
                return ResultDTO.Fail($"Error updating topic: {ex.Message} ");
            }
        }
        public async Task<ResultDTO> Delete(uint id)
        {
            if (id <= 0)
            {
                return ResultDTO.Fail("Invalid Topic ID");
            }
            try
            {
                return await topicsDB.Delete(id);
            }
            catch (Exception ex)
            {
                return ResultDTO.Fail($"Error deleting topic:  {ex.Message}");
            }
        }

        public async Task<ResultDTO> GetById(int id)
        {
            if (id <= 0)
            {
                return ResultDTO.Fail("Invalid Topic ID");
            }
            try
            {
                return await topicsDB.GetById(id); ;
            }
            catch (Exception ex)
            {
                return ResultDTO.Fail($"Error get topic by id:  {ex.Message}");
            }
        }

        public async Task<List<TopicDTO>?> GetAll()
        {
            try
            {
                ResultDTO result = await topicsDB.GetAll();
                if (result != null && result.Data != null)
                {
                    return result.Data as List<TopicDTO>;
                }
                return null;
            }
            catch 
            {
                return null;
            }
            
        }
    }
}

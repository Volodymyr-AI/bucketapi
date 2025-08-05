namespace Order.WebApi.CustomMiddlewares.RateLimiter;

public class TokenBucket
{
    private readonly int _maxTokens;
    private readonly TimeSpan _refillPeriod;
    private readonly object _lock = new();
    private int _tokens;
    private DateTime _lastRefill;
    
    public int Tokens =>  _tokens;

    public TokenBucket(int maxTokens, TimeSpan refillPeriod)
    {
         _maxTokens = maxTokens;
         _refillPeriod = refillPeriod;
         _tokens = maxTokens;
         _lastRefill = DateTime.UtcNow;
    }

    public bool TryConsume(int tokensToConsume = 1)
    {
        lock (_lock)
        {
            RefillTokens();

            if (_tokens >= tokensToConsume)
            {
                _tokens -= tokensToConsume;
                return true;
            }
            
            return false;
        }
    }

    private void RefillTokens()
    {
        var now = DateTime.UtcNow;
        var timePassed = now - _lastRefill;

        if (timePassed >= _refillPeriod)
        {
            _tokens = _maxTokens;
            _lastRefill = now;
        }
        else
        {
            var refillRatio = timePassed.TotalMilliseconds / _refillPeriod.TotalMilliseconds;
            var tokensToAdd = (int)(_maxTokens * refillRatio);

            if (tokensToAdd > 0)
            {
                _tokens += Math.Min(_maxTokens, _tokens + tokensToAdd);
                _lastRefill = now;
            }
        }
    }
}
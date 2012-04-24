namespace JabbR.Models.Migrations
{
    using System.Data.Entity.Migrations.Infrastructure;
    
    public partial class Initial : IMigrationMetadata
    {
        string IMigrationMetadata.Id
        {
            get { return "201111240945284_Initial"; }
        }
        
        string IMigrationMetadata.Source
        {
            get { return null; }
        }
        
        string IMigrationMetadata.Target
        {
            get { return "H4sIAAAAAAAEAOy9B2AcSZYlJi9tynt/SvVK1+B0oQiAYBMk2JBAEOzBiM3mkuwdaUcjKasqgcplVmVdZhZAzO2dvPfee++999577733ujudTif33/8/XGZkAWz2zkrayZ4hgKrIHz9+fB8/Iv7Hv/cffPx7vFuU6WVeN0W1/Oyj3fHOR2m+nFazYnnx2Ufr9nz74KPf4+g3Th6fzhbv0p807fbQjt5cNp99NG/b1aO7d5vpPF9kzXhRTOuqqc7b8bRa3M1m1d29nZ2Du7s7d3MC8RHBStPHr9bLtljk/Af9eVItp/mqXWflF9UsLxv9nL55zVDTF9kib1bZNP/so5N51o6l2UfpcVlkhMLrvDx/T3x2HgKfj2xP1Ncp4dRev7le5dyfdPWqqoJW1O73yq+DD+ijl3W1yuv2+lV+ru9So4/Su+GLd7tv2vf8l4DAZx+dLdt7ex+lL9ZlmU1K+uA8K5v8o3T16aPXbVXnn+fLvM7afPYya9u8pjk5m+U8ACXEo9Wnt6PFw7s7e6DF3Wy5rNqspQnuYd7B83nWtMfTtrjk7gThp4TMG5rTCM43A3uxnl3ks02g2np9IyT8a2C8bmti4Y/SZ8W7fPY8X160c4vPF9k78wn9+lH61bIgjred3NDpi+yyuGAydbr/Im+a7CIntnyVl9ygmRcr4c4xeEkb/P7MU+mzulq8qtBN97vf/01WX+QtDaUaaPC6WtfT90Dtq4YkdxAvfMtwmy5W7psoTt7XMYwe33UidaOg6fh+JGsdPEk3ttTZzz5fd/o9s+L4Q+vyu/N82dUBX56fN2C7m5TKIOuLsH2oRBr2HpRIIx63RQuicyNa0iiOFsveJrS4QQyt9xJLRuFHMvlzLhs/HNvW6fTbWTP/oXfKjoUd65OKGDtb3qwA/r/hopyUBXHpD4N/NirED/QFYmon4ip8LaVDgcYXeZvNsjb7OnoHpH1fteOm4//VWoeDnh+CSA5N1XHTVNOCUe37bWJCQ/xPl7P0JnsqA+kYPkJ/XbbFqiymhMdnH32rR5oNoK2jGoIWEx/C3RmPd7tj90Z58+A9admEX0x0QuzE0r/fqGPO+c1j7kB9zwEHnsltpiR0U77R2Q4cnFvQ8z1mWwQArndWkMQrAb6TTSY1++Pv2t7g8cLrvA2VrBOk3qz0hhgC8AY6BMeS7wZQGvxFYQidbgDgqeQYHF9jd0F59I2OTT1vr9mQd95V/bdQLnYcXXKGSN5OnXjAdHK7ZiYc6y3p4NvbOBWGdMxttIyHtHLBDUOP6JSfpYGHMc5mBuhrm9vpmw9lgEDD3ETLDXQwFtUqFPvd47uS4dQPHt8dSIU+/iJbrcjYe6lR/SR9rXnR7dfvnwJdCIy704C3uurP9kQuEJGl8y1SFLP8WVE3LXnF2SSD+3EyW/Sa3UZ9mq58LdqfK6OATGv87jhLs8NO13YAONo9o+EsyH/jkem4LLfEX02Rk87KrI6FridVuV4sBwPgTe+H8YoPKPzm/SCaoKULjz/vecqboEn86cORT/oQHt/tULc7f557qS07ctTlhlvxSiDgH8Iy0UQkQ7kN1wy+/bPFODY/6MOwH94ezlmHTfqB1Ka3JXXnvy+f/L+GPURlfwhfxIzQLZki/urPFkd82EzeXtCHIEiI6kOQT24PwaSAfBjms9tD+X+7SnW5oEB07af/r5GdwP//uiI0mNdhKDdJ0ca3f3YEgfHu87L38Q9/gkIPc9AAalR1k40z7d7DjsFR7rLxQMTUJ87ttB3DiKo8UMf2/jUQ07Dg6yL2/jiRGZ4VnEE5a5Bxs9m524y3G098LYbww8vNps20GrJfQy7xAOH7wejXJbuF9v70vxG1D2RVQ5P3Re0bmdowgL5Z1qXdNyfrfnD8dQn4jc5rLAXwdRF7f5xuJetD472ZIXophG4Ta3r0E/u3TSFo+C6s4d6TBAQTotFUQjeelyYfpTT2y2KGWP71ddPmizEajF//olKcFtfgi2xZnOdN+6Z6m1NCB+kGyvGURdZIdue9MhV2qaRpZmUkT+EtHsWV1O1Wjj50xboAAW5cJfoaK4/dZUxygvK2+EaWMWOgbrOMiX8NjOVlVk/nWb21yN7d+XorS9F5jGql/w9PpY3J35tsfVhuxfKDwEiMHrJCdX7eIN35nqNzZtAj4fth45TjLUG8FztFVqz+v8tL39D8f00x7gPyV6U/CJCJ8gXUpHh/Rvx/qcZ0Yf17U+nWbD4YJN+O0/vh8c2M7gb0s8LnXrD9s0e2zRHO7Wh3Q5ASf2E48ruJ7GF3UXV5I337WNwWzhBlPR+2b8cjq7idVTeXtOgtln9xw0J+D5ZdG++As5+HEL/VA0czldfg2qwkw920NTndvWzOy7pYTotVVnZH0WkY5YCBEMPC7H7zNF/lSwhSZJi36W9ToGphd/jwJioEccvtWSFii/vL0L25kw//P8QKAysGP/essCnstrB/dlnhhvzQ1+WH3V4S48vl07zM2zyFU1CRMTrJmmk265seBPc34TCopsIv///BUZuyftFObzCCPwesFUtPfV2r8/9z1rr1LP+csdYP1YCJh2OzbQaNbmasx1qaWO2g/VHqPKYem0l2jUKLSUWTL16X+baJGLd+F9aURXux3w51pA1u15do3WhHmmEe6AXf3tyFH8P0ewm+jXUUNLjlaG6YpLDJptENT1qY3R1yjlOvWX/+Ig70gDKzA/U/7MnMgJfUedl+3lVp4ZDec7jCKZuHG7N1Ubegg7F8+P+K4fadnIEx3+ANfYMDjzF8fJ3vGyeBGuObSRBfVPrGWP1ngwS95Rn73eO7ojL0A/qztwxDZmq9RFJJ/nqaN8WFA4GVpWXOHoYDatqcLc8rYy07GJkm3ZSKqsfjui3Os2lLX0+J6Wmh6KP0J7NyDSW6mOSzs+WX63a1bmnI+WJSBlYX9nZT/4/v9nB+/OUKf3nrTl9/CIRmgTzcl8sn66KcWbyfRVIVAyBgyDVPhblska+6uLaQXlTLWwJS8j01/sebfLEqCVjz5fJ1hkzmEG430zCk2OOnRXZRZwufgvKJYvI6o569LqgD/w3XH/1J7DpbvDv6fwIAAP//bYmGpQFCAAA="; }
        }
    }
}

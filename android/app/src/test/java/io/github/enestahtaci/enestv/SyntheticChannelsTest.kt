package io.github.enestahtaci.enestv

import org.junit.Assert.assertEquals
import org.junit.Test

class SyntheticChannelsTest {
    @Test
    fun createsRequestedCountWithTurkishNames() {
        val channels = SyntheticChannels.create(10_000)
        assertEquals(10_000, channels.size)
        assertEquals("ŞİMDİ TV 1", channels[0])
        assertEquals("Sinema Aile 1000", channels[9_999])
    }
}

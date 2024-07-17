<CsoundSynthesizer>
<CsOptions>
;-odac
 ; -m0d
</CsOptions>
<CsInstruments>

;sr = 44100  ; Sample rate
ksmps = 32  ; Control signal samples per audio signal sample
nchnls = 1  ; Number of audio channels
0dbfs	=	1

instr 1
    iFreq = p4
    iAttack = p5
    iDecay = p6
    iSustain = p7
    iRelease = p8
    iGain = p9

    ;kFreq chnget "freq"

    asig oscili 1.0, iFreq ; sine oscillator generates signal

    iAmp init 1.0 ; initial amplitude

    ; Steps through segments while a gate from midikey2 in the score file is set to turnon 
    kEnv linsegr 0, iAttack, iAmp, iDecay, iSustain, -1, iSustain, iRelease, 0   ; -1 means hold until turnoff

    out iGain * asig * kEnv ; sum signals, apply gain 'iGain', and output
endin

</CsInstruments>
<CsScore>
; Play Instrument 1 infinitely
;i1 0 z
</CsScore>
</CsoundSynthesizer>
